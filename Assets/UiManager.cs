﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IUiEventProcessor
{
    bool ProcessEvent(Event e);
}

public interface IUiEventProcessorBackground { }

public class UiManager : MonoBehaviour
{
    private static UiManager _Instance = null;
    public static UiManager Instance
    {
        get
        {
            if (_Instance == null) _Instance = FindObjectOfType<UiManager>();
            return _Instance;
        }
    }

    // each Window occupies certain Z position in the interface layer. Interface layer is a 1..10 field, and a window occupies approximately 0.05 in this field.
    // shadow/background is 0.00, element shadows are 0.02, elements are 0.03, element overlays are 0.04.
    // as such, there's a maximum of 200 windows at once.
    public float TopZ = MainCamera.InterfaceZ;
    private List<MonoBehaviour> Windows = new List<MonoBehaviour>();

    private void UpdateTopZ()
    {
        TopZ = MainCamera.InterfaceZ;
        foreach (MonoBehaviour wnd in Windows)
        {
            if (wnd.transform.position.z - 0.05f < TopZ)
                TopZ = wnd.transform.position.z - 0.05f;
        }
    }

    public float RegisterWindow(MonoBehaviour wnd)
    {
        Windows.Add(wnd);
        UpdateTopZ();
        return TopZ;
    }

    public void UnregisterWindow(MonoBehaviour wnd)
    {
        Windows.Remove(wnd);
        UpdateTopZ();
    }

    public void ClearWindows()
    {
        foreach (MonoBehaviour wnd in Windows)
            Destroy(wnd);
        Windows.Clear();
    }

    void Start()
    {

    }

    private bool GotProcessors = false;
    private List<MonoBehaviour> Processors = new List<MonoBehaviour>();
    private List<bool> ProcessorsEnabled = new List<bool>();


    private float lastMouseX = 0;
    private float lastMouseY = 0;
    private float lastMouseChange = -1;
    void Update()
    {
        if (lastMouseChange >= 0)
            lastMouseChange += Time.unscaledDeltaTime;

        GotProcessors = false;
        EnumerateObjects();
        // get all events.
        Event e = new Event();
        while (Event.PopEvent(e))
        {
            // pressing PrintScreen or Alt+S results in screenshot unconditionally.
            if (e.type == EventType.KeyDown &&
                (e.keyCode == KeyCode.Print ||
                 e.keyCode == KeyCode.SysReq ||
                (e.keyCode == KeyCode.S && e.alt)))
            {
                MainCamera.Instance.TakeScreenshot();
                return;
            }

            // reverse iteration
            bool EventIsGlobal = (e.type == EventType.KeyUp ||
                                  e.rawType == EventType.MouseUp);
            for (int i = Processors.Count - 1; i >= 0; i--)
            {
                // check if processor's renderer is enabled. implicitly don't give any events to invisible objects.
                if (!ProcessorsEnabled[i]) continue;
                if (((IUiEventProcessor)Processors[i]).ProcessEvent(e) && !EventIsGlobal)
                    break;
            }
        }

        // also fake mouse event for each processor
        Vector2 mPos = Utils.GetMousePosition();
        /*if (mPos.x != lastMouseX ||
            mPos.y != lastMouseY)*/
        {
            Event ef = new Event();
            ef.type = EventType.MouseMove;
            for (int i = Processors.Count - 1; i >= 0; i--)
            {
                // check if processor's renderer is enabled. implicitly don't give any events to invisible objects.
                if (!ProcessorsEnabled[i]) continue;
                if (((IUiEventProcessor)Processors[i]).ProcessEvent(ef))
                    break;
            }

            if (lastMouseX != mPos.x ||
                lastMouseY != mPos.y)
            {
                lastMouseX = mPos.x;
                lastMouseY = mPos.y;
                lastMouseChange = 0;
                UnsetTooltip();
            }
        }

        // send fake event if user held mouse at same position for certain time
        if (lastMouseChange > 1) // 1 second = timeout for tooltip
        {
            // 
            Event ef = new Event();
            ef.type = EventType.ExecuteCommand;
            ef.commandName = "tooltip";

            for (int i = Processors.Count - 1; i >= 0; i--)
            {
                // check if processor's renderer is enabled. implicitly don't give any events to invisible objects.
                if (!ProcessorsEnabled[i]) continue;
                if (((IUiEventProcessor)Processors[i]).ProcessEvent(ef))
                    break;
            }

            lastMouseChange = -1;
        }
    }

    // TOOLTIP RELATED
    private GameObject Tooltip;
    private MeshRenderer TooltipRenderer;
    private MeshFilter TooltipFilter;
    private AllodsTextRenderer TooltipRendererA;
    private Texture2D TooltipBall;
    private Utils.MeshBuilder TooltipBuilder = new Utils.MeshBuilder();

    public void SetTooltip(string text)
    {
        if (Tooltip == null)
        {
            TooltipRendererA = new AllodsTextRenderer(Fonts.Font2, Font.Align.Left, 0, 0, false);
            Tooltip = Utils.CreateObject();
            Tooltip.transform.parent = transform;
            TooltipRenderer = Tooltip.AddComponent<MeshRenderer>();
            TooltipFilter = Tooltip.AddComponent<MeshFilter>();
            TooltipBall = Images.LoadImage("graphics/interface/ball.bmp", 0, Images.ImageType.AllodsBMP);

            Material[] materials = new Material[] { new Material(MainCamera.MainShader), new Material(MainCamera.MainShader)};
            materials[0].mainTexture = TooltipBall;
            TooltipRenderer.materials = materials;

            GameObject TooltipText = TooltipRendererA.GetNewGameObject(0.01f, Tooltip.transform, 100);
            TooltipRendererA.Material.color = new Color32(165, 121, 49, 255);
            TooltipText.transform.localPosition = new Vector3(6, 6, -0.02f);
        }

        Tooltip.SetActive(true);

        float topX = lastMouseX;
        float topY = lastMouseY;

        text = text.Replace('#', '\n').Replace("~", "");
        TooltipRendererA.Text = text;

        // ideal position for the tooltip is top/right of the mouse.
        // but if it doesn't fit, should be moved around.
        topX = lastMouseX;
        topY = lastMouseY - TooltipRendererA.Height - 12;

        float fw = TooltipRendererA.ActualWidth + 12;
        float fh = TooltipRendererA.Height + 12;

        if (topX + fw > Screen.width)
            topX = Screen.width - fw;
        if (topY + fh > Screen.height)
            topY = Screen.height - fh;
        if (topX < 0)
            topX = 0;
        if (topY < 0)
            topY = 0;

        Tooltip.transform.localPosition = new Vector3(topX, topY, MainCamera.MouseZ + 0.01f);

        TooltipBuilder.Reset();
        TooltipBuilder.AddQuad(0, 0, 0, 4, 4);
        TooltipBuilder.AddQuad(0, TooltipRendererA.ActualWidth + 8, 0, 4, 4);
        TooltipBuilder.AddQuad(0, TooltipRendererA.ActualWidth + 8, TooltipRendererA.Height + 8, 4, 4);
        TooltipBuilder.AddQuad(0, 0, TooltipRendererA.Height + 8, 4, 4);

        // now render border quads
        float bw = TooltipRendererA.ActualWidth + 6;
        float bh = TooltipRendererA.Height + 6;
        // top border bright
        TooltipBuilder.CurrentMesh = 1;
        TooltipBuilder.CurrentColor = new Color32(165, 121, 49, 255);
        TooltipBuilder.CurrentPosition = new Vector3(3, 1);
        TooltipBuilder.NextVertex();
        TooltipBuilder.CurrentColor = new Color32(165, 121, 49, 255);
        TooltipBuilder.CurrentPosition = new Vector3(3 + bw, 1);
        TooltipBuilder.NextVertex();
        // top border dark
        TooltipBuilder.CurrentColor = new Color32(82, 60, 24, 255);
        TooltipBuilder.CurrentPosition = new Vector3(3 + bw, 1 + 2);
        TooltipBuilder.NextVertex();
        TooltipBuilder.CurrentColor = new Color32(82, 60, 24, 255);
        TooltipBuilder.CurrentPosition = new Vector3(3, 1 + 2);
        TooltipBuilder.NextVertex();
        // bottom border bright
        TooltipBuilder.CurrentMesh = 1;
        TooltipBuilder.CurrentColor = new Color32(165, 121, 49, 255);
        TooltipBuilder.CurrentPosition = new Vector3(3, 3 + bh);
        TooltipBuilder.NextVertex();
        TooltipBuilder.CurrentColor = new Color32(165, 121, 49, 255);
        TooltipBuilder.CurrentPosition = new Vector3(3 + bw, 3 + bh);
        TooltipBuilder.NextVertex();
        // bottom border dark
        TooltipBuilder.CurrentColor = new Color32(82, 60, 24, 255);
        TooltipBuilder.CurrentPosition = new Vector3(3 + bw, 3 + bh + 2);
        TooltipBuilder.NextVertex();
        TooltipBuilder.CurrentColor = new Color32(82, 60, 24, 255);
        TooltipBuilder.CurrentPosition = new Vector3(3, 3 + bh + 2);
        TooltipBuilder.NextVertex();
        // left border bright
        TooltipBuilder.CurrentMesh = 1;
        TooltipBuilder.CurrentColor = new Color32(165, 121, 49, 255);
        TooltipBuilder.CurrentPosition = new Vector3(1, 3);
        TooltipBuilder.NextVertex();
        TooltipBuilder.CurrentColor = new Color32(165, 121, 49, 255);
        TooltipBuilder.CurrentPosition = new Vector3(1, 3 + bh);
        TooltipBuilder.NextVertex();
        // left border dark
        TooltipBuilder.CurrentColor = new Color32(82, 60, 24, 255);
        TooltipBuilder.CurrentPosition = new Vector3(1 + 2, 3 + bh);
        TooltipBuilder.NextVertex();
        TooltipBuilder.CurrentColor = new Color32(82, 60, 24, 255);
        TooltipBuilder.CurrentPosition = new Vector3(1 + 2, 3);
        TooltipBuilder.NextVertex();
        // right border bright
        TooltipBuilder.CurrentMesh = 1;
        TooltipBuilder.CurrentColor = new Color32(165, 121, 49, 255);
        TooltipBuilder.CurrentPosition = new Vector3(3 + bw, 3);
        TooltipBuilder.NextVertex();
        TooltipBuilder.CurrentColor = new Color32(165, 121, 49, 255);
        TooltipBuilder.CurrentPosition = new Vector3(3 + bw, 3 + bh);
        TooltipBuilder.NextVertex();
        // right border dark
        TooltipBuilder.CurrentColor = new Color32(82, 60, 24, 255);
        TooltipBuilder.CurrentPosition = new Vector3(3 + bw + 2, 3 + bh);
        TooltipBuilder.NextVertex();
        TooltipBuilder.CurrentColor = new Color32(82, 60, 24, 255);
        TooltipBuilder.CurrentPosition = new Vector3(3 + bw + 2, 3);
        TooltipBuilder.NextVertex();
        // BACKGROUND QUAD
        TooltipBuilder.AddQuad(TooltipBuilder.CurrentMesh, 3, 3, bw, bh, new Color32(33, 44, 33, 255));

        TooltipFilter.mesh = TooltipBuilder.ToMesh(MeshTopology.Quads, MeshTopology.Quads);
    }

    public void UnsetTooltip()
    {
        if (Tooltip != null) Tooltip.SetActive(false);
    }

    public void Subscribe(IUiEventProcessor mb)
    {
        if (!Processors.Contains((MonoBehaviour)mb))
            Processors.Add((MonoBehaviour)mb);
    }

    public void Unsubscribe(IUiEventProcessor mb)
    {
        Processors.Remove((MonoBehaviour)mb);
    }

    void EnumerateObjects()
    {
        if (GotProcessors) return;
        Processors.Sort((a, b) => b.transform.position.z.CompareTo(a.transform.position.z));

        ProcessorsEnabled.Clear();
        int enc = 0;
        foreach (MonoBehaviour mb in Processors)
        {
            if (mb is IUiEventProcessorBackground)
            {
                ProcessorsEnabled.Add(true);
                enc++;
                continue;
            }

            // check if object has any visible parts
            List<Renderer> renderers = mb.gameObject.GetComponentsInChildren<Renderer>().Concat(mb.gameObject.GetComponents<Renderer>()).ToList();
            bool isEnabled = renderers.Any(render => render.enabled);

            ProcessorsEnabled.Add(isEnabled);
            if (isEnabled)
                enc++;
        }

        GotProcessors = true;
    }
}