using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using System.Threading;
using System.IO;

public class GameManager : MonoBehaviour // The ":" means that GameManager inherits from MonoBehaviour
{
    // A Delegate declaration determines the methods that can be referenced by the delegate
    //      In this case the delager is used to reference any methods that has no input parameter
    //      and returns a bool type variable
    public delegate bool LoadCoroutine();

    // A static variable has a single copy of the variable created and shared
    // among all objects at the class level.
    // This is a variable of type GameManager set to null
    private static GameManager _Instance = null;

    // This is a read property (since it only has get) that allows the user to access the
    // private variable '_Instance'
    // Note:    A property is a method that provides a flexible mechanism to read, write,
    //          or compute the value of a private field
    public static GameManager Instance
    {
        // The 'get' keyword defines an accessor method in a property or indexer
        // that returns the property value or the indexer element
        get
        {
            if (_Instance == null) _Instance = FindObjectOfType<GameManager>();
            return _Instance;
        }
    }

    private static bool CheckServerConfig()
    {
        if (ResourceManager.FileExists("server.cfg"))
        {
            StringFile sf = new StringFile("server.cfg"); // Searched for "server.cfg" file
            // execute all commands from there
            foreach (string cmd in sf.Strings)
            {
                Debug.Log(cmd);
                GameConsole.Instance.ExecuteCommand(cmd);
            }
            return true;
        }

        return false;
    }

    // since this is a part of global state
    private bool _IsHeadlessChecked = false;
    private bool _IsHeadless = false;
    public bool IsHeadless // Another property to access a private bool _IsHeadless
    {
        get
        {
            if (!_IsHeadlessChecked)
            {
                string[] args = Environment.GetCommandLineArgs();
                if (args.Contains("-nographics") || args.Contains("-batchmode"))
                    _IsHeadless = true;
                _IsHeadlessChecked = true;
            }

            return _IsHeadless;
        }
    }

    public MapView MapView;
    public GameConsole GameConsole;

    void Awake()
    {
        Debug.LogFormat("GameManager.Awake\n");
        // system Unity configuration
        QualitySettings.vSyncCount = 0; // 0 =  Do nothing
        Application.targetFrameRate = Application.isBatchMode ? 10 : 60; // True:10, else 60

        pMainThreadId = Thread.CurrentThread.ManagedThreadId;
        _Instance = this; // force set. through this field, other threads will access mapview.
        Locale.InitLocale(); // load locale strings, like main.txt, patch.txt, etc
    }

    void Start()
    {
        Debug.LogFormat("GameManager.Start\n");
        GameConsole = Utils.CreateObjectWithScript<GameConsole>();
        GameConsole.transform.parent = UiManager.Instance.transform;
    }

    void OnDestroy()
    {

    }

    private IEnumerator DelegateCoroutine(LoadCoroutine del)
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            if (!del())
                break;
        }
    }

    private int pMainThreadId = -1;
    private List<LoadCoroutine> pDelegates = new List<LoadCoroutine>();

    public void CallDelegateOnNextFrame(LoadCoroutine del)
    {
        if (pMainThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            bool dFirst = del();
            if (dFirst)
                StartCoroutine(DelegateCoroutine(del));
            return;
        }

        lock (pDelegates)
            pDelegates.Add(del);
    }

    private Thread ClassLoadThread = null;
    private bool ClassLoadThreadDone = false;
    private void ClassLoadThreadProc()
    {
        try
        {
            TemplateLoader.LoadTemplates();
            ObstacleClassLoader.InitClasses();
            StructureClassLoader.InitClasses();
            UnitClassLoader.InitClasses();
            ItemClassLoader.InitClasses();
            ProjectileClassLoader.InitClasses();
            ClassLoadThreadDone = true;
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat("Exception while loading classes.\n{0}", e.ToString());
            ClassLoadThreadDone = true;
        }
    }

    void Update()
    {
        // initiate resource load.
        if (!ClassLoadThreadDone)
        {   
            Debug.LogFormat("GameManager.Update_1\n");
            GameConsole.ConsoleEnabled = false;
            MapView.gameObject.SetActive(false);
            MouseCursor.SetCursor(MouseCursor.CurWait);

            if (ClassLoadThread == null)
            {
                ClassLoadThread = new Thread(new ThreadStart(ClassLoadThreadProc));
                ClassLoadThread.Start();
            }
        }
        else if (ClassLoadThreadDone && ClassLoadThread != null)
        {
            Debug.LogFormat("GameManager.Update_2\n");
            GameConsole.ConsoleEnabled = true;
            MapView.gameObject.SetActive(true);
            ClassLoadThread = null;

            MouseCursor.SetCursor(MouseCursor.CurDefault);
            Config.Load();
            CheckServerConfig();
        }

        lock (pDelegates)
        {
            Debug.LogFormat("GameManager.Update_3\n");
            for (int i = 0; i < pDelegates.Count; i++)
                StartCoroutine(DelegateCoroutine(pDelegates[i]));
            pDelegates.Clear();
        }
    }
}
