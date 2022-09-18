﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class ObstacleClass
{
    public readonly static int MagicIntNull = unchecked((int)0xDEADFACE);

    public struct AnimationFrame
    {
        public int Time;
        public int Frame;
    }

    public string DescText = null;
    public int ID = MagicIntNull;
    public ObstacleFile File = null;
    public int Index = MagicIntNull;
    public float CenterX = -2;
    public float CenterY = -2;
    public AnimationFrame[] Frames = null;
    public int DeadObject = MagicIntNull;
    public int Parent = MagicIntNull;
}

public class ObstacleFile
{
    public Images.AllodsSpriteSeparate File = null;
    public Images.AllodsSpriteSeparate FileB = null;
    public string FileName = "";
    private bool Loaded = false;

    public void UpdateSprite()
    {
        if (!Loaded)
        {
            File = Images.Load256Separate(FileName + ".256");
            FileB = Images.Load256Separate(FileName + "b.256");
            Loaded = true;
        }
    }
}

public class ObstacleClassLoader
{
    internal static bool ClassesLoaded = false;
    internal static List<ObstacleFile> Files = new List<ObstacleFile>();
    internal static List<ObstacleClass> Classes = new List<ObstacleClass>();

    public static ObstacleClass GetObstacleClassById(int typeId)
    {
        foreach (ObstacleClass cls in Classes)
            if (cls.ID == typeId)
                return cls;

        return null;
    }

    public static ObstacleClass GetObstacleClassByName(string name)
    {
        name = name.ToLower();
        foreach (ObstacleClass cls in Classes)
            if (cls.DescText.ToLower() == name)
                return cls;

        return null;
    }

    public static void InitClasses()
    {
        if (ClassesLoaded)
            return;
        ClassesLoaded = true;
        Registry reg = new Registry("graphics/objects/objects.reg");
        int ObjectCount = reg.GetInt("Global", "ObjectCount", 0);
        int FileCount = reg.GetInt("Global", "FileCount", 0);
        for (int i = 0; i < FileCount; i++)
        {
            string filename = reg.GetString("Files", string.Format("File{0}", i), "");
            ObstacleFile file = new ObstacleFile();
            file.FileName = "graphics/objects/" + filename.ToLower().Replace('\\', '/'); // Dorath Modified
            Files.Add(file);
        }

        for (int i = 0; i < ObjectCount; i++)
        {
            string on = string.Format("Object{0}", i);
            ObstacleClass cls = new ObstacleClass();

            //cls.DescText = RegistryUtil.GetWithParent<string>(reg, on, "DescText", "");
            cls.DescText = reg.GetString(on, "DescText", "");
            cls.ID = reg.GetInt(on, "ID", ObstacleClass.MagicIntNull);
            //int file = RegistryUtil.GetWithParent<int>(reg, on, "File", -1);
            int file = reg.GetInt(on, "File", ObstacleClass.MagicIntNull);
            if (file >= 0 && file < Files.Count)
                cls.File = Files[file];
            //cls.Index = RegistryUtil.GetWithParent<int>(reg, on, "Index", 0);
            cls.Index = reg.GetInt(on, "Index", ObstacleClass.MagicIntNull);
            int centerx = reg.GetInt(on, "CenterX", -1);
            int centery = reg.GetInt(on, "CenterY", -1);
            int width = reg.GetInt(on, "Width", 128);
            int height = reg.GetInt(on, "Height", 128);
            cls.CenterX = (centerx < 0) ? -2 : (float)centerx / width;
            cls.CenterY = (centerx < 0) ? -2 : (float)centery / height;
            cls.DeadObject = reg.GetInt(on, "DeadObject", ObstacleClass.MagicIntNull);
            cls.Parent = reg.GetInt(on, "Parent", ObstacleClass.MagicIntNull);
            int phases = reg.GetInt(on, "Phases", ObstacleClass.MagicIntNull);
            if (phases == 1)
            {
                cls.Frames = new ObstacleClass.AnimationFrame[1];
                cls.Frames[0].Frame = 0;
                cls.Frames[0].Time = 0;
            }
            else
            {
                int[] animationtime = reg.GetArray(on, "AnimationTime", null);
                int[] animationframe = reg.GetArray(on, "AnimationFrame", null);
                if (animationtime != null && animationframe != null &&
                    animationtime.Length == animationframe.Length)
                {
                    cls.Frames = new ObstacleClass.AnimationFrame[animationtime.Length];
                    for (int j = 0; j < cls.Frames.Length; j++)
                    {
                        cls.Frames[j].Time = animationtime[j];
                        cls.Frames[j].Frame = animationframe[j];
                    }
                }
                else if (phases > 0)
                {
                    cls.Frames = new ObstacleClass.AnimationFrame[1];
                    cls.Frames[0].Frame = 0;
                    cls.Frames[0].Time = 0;
                }
                else
                {
                    cls.Frames = null;
                }
            }

            //Debug.Log(string.Format("object {0} ({1}) frames = {2}", on, cls.DescText.Trim(), cls.Frames.Length));

            Classes.Add(cls);
        }

        foreach (ObstacleClass cls in Classes)
        {
            int id = cls.Parent;
            while (id != -1)
            {
                ObstacleClass clsp = null;
                foreach (ObstacleClass clsp_ in Classes)
                {
                    if(clsp_.ID == id)
                    {
                        clsp = clsp_;
                        break;
                    }
                }

                if (clsp == null)
                    break;

                if (cls.Frames == null)
                    cls.Frames = clsp.Frames;
                if (cls.DeadObject == ObstacleClass.MagicIntNull)
                    cls.DeadObject = clsp.DeadObject;
                if (cls.Index == ObstacleClass.MagicIntNull)
                    cls.Index = clsp.Index;
                if (cls.CenterX == -2)
                    cls.CenterX = clsp.CenterX;
                if (cls.CenterY == -2)
                    cls.CenterY = clsp.CenterY;

                id = clsp.Parent;
            }
        }

        foreach (ObstacleClass cls in Classes)
        {
            if (cls.Index == ObstacleClass.MagicIntNull)
                cls.Index = 1;
            if (cls.DeadObject == ObstacleClass.MagicIntNull)
                cls.DeadObject = -1;
            if (cls.Frames == null)
            {
                cls.Frames = new ObstacleClass.AnimationFrame[1];
                cls.Frames[0].Frame = 0;
                cls.Frames[0].Time = 0;
            }
            if (cls.CenterX == -2)
                cls.CenterX = 0;
            if (cls.CenterY == -2)
                cls.CenterY = 0;
        }
    }

    private static int LastLoaded = 0;
    public static float LoadSprites(float time)
    {
        InitClasses();

        float timestart = Time.realtimeSinceStartup;
        for (; LastLoaded < Files.Count; LastLoaded++)
        {
            Files[LastLoaded].UpdateSprite();
            if (Time.realtimeSinceStartup - timestart > time)
                break;
        }

        return (float)LastLoaded / Files.Count;
    }
}
