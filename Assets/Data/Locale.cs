using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

public class Locale
{
    public static List<string> Main;
    public static List<string> Dialogs;
    public static List<string> Building;
    public static List<string> UnitName;

    public static List<string> ItemServ;
    public static List<string> ItemName;
    public static List<string> Stats;

    public static List<string> Spell; // spells by id
    public static List<string> Spells; // spell tooltips for spellbook

    public static List<string> Patch;

    private static bool Initialized = false;
    public static void CheckLocale()
    {
        if (Initialized)
            return;
        InitLocale();
    }

    public static void InitLocale()
    {
        Initialized = true;

        Main = new StringFile("locale/en/main.txt").Strings;
        Dialogs = new StringFile("locale/en/dialogs.txt").Strings;
        Building = new StringFile("locale/en/building.txt").Strings;
        UnitName = new StringFile("locale/en/unitname.txt").Strings;

        ItemServ = new StringFile("locale/en/itemserv.txt").Strings;
        ItemName = new StringFile("locale/en/itemname.txt").Strings;
        Stats = new StringFile("locale/en/stats.txt").Strings;

        Spell = new StringFile("locale/en/spell.txt").Strings;
        Spells = new StringFile("locale/en/spells.txt").Strings;

        Patch = new StringFile("locale/en/patch.txt").Strings;
    }

    private static Dictionary<string, List<string>> ListsByName;
    public static string TranslateString(string translateStr)
    {
        CheckLocale();

        string translated = "<invalid translation>";
        
        // should consist of a string + index
        int trBeginOffset = translateStr.IndexOf('[');
        if (trBeginOffset < 0)
            return translated;
        int trEndOffset = translateStr.IndexOf(']');
        if (trEndOffset < 0)
            return translated;
        string trIndexStr = translateStr.Substring(trBeginOffset + 1, trEndOffset - trBeginOffset - 1);
        int trIndex;
        if (!int.TryParse(trIndexStr, out trIndex))
            return translated;
        if (trIndex < 0)
            return translated;

        string trType = translateStr.Substring(0, trBeginOffset).ToLowerInvariant();

        // produce new dict if needed
        if (ListsByName == null)
        {
            ListsByName = new Dictionary<string, List<string>>();
            FieldInfo[] fields = typeof(Locale).GetFields();
            foreach (FieldInfo fld in fields)
            {
                if (fld.IsStatic && fld.FieldType == typeof(List<string>))
                {
                    ListsByName[fld.Name.ToLowerInvariant()] = (List<string>)fld.GetValue(null);
                }
            }
        }

        if (!ListsByName.ContainsKey(trType))
            return translated;

        List<string> trList = ListsByName[trType];
        if (trIndex >= trList.Count)
            return translated;

        translated = trList[trIndex];
        return translated;
    }
}
