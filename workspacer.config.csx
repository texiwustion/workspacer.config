#r "C:\Users\w6666\scoop\apps\workspacer\current\workspacer.Shared.dll"
#r "C:\Users\w6666\scoop\apps\workspacer\current\plugins\workspacer.Bar\workspacer.Bar.dll"
#r "C:\Users\w6666\scoop\apps\workspacer\current\plugins\workspacer.Gap\workspacer.Gap.dll"
#r "C:\Users\w6666\scoop\apps\workspacer\current\plugins\workspacer.ActionMenu\workspacer.ActionMenu.dll"
#r "C:\Users\w6666\scoop\apps\workspacer\current\plugins\workspacer.FocusIndicator\workspacer.FocusIndicator.dll"

using System;
using System.Collections.Generic;
using System.Linq;
using workspacer;
using workspacer.Bar;
using workspacer.Bar.Widgets;
using workspacer.Gap;
using workspacer.ActionMenu;
using workspacer.FocusIndicator;

Action<IConfigContext> doConfig = (context) =>
{
    // Uncomment to switch update branch (or to disable updates)
    context.Branch = Branch.None;

    context.AddBar();
    context.AddFocusIndicator();
    var actionMenu = context.AddActionMenu();

    context.WorkspaceContainer.CreateWorkspaces("1", "2", "3", "4", "5");
    context.CanMinimizeWindows = true; // false by default
};

return new Action<IConfigContext>((IConfigContext context) =>
{
    // Uncomment to switch update branch (or to disable updates)
    context.Branch = Branch.None;
    
    /* Variables */
    var fontSize = 9;
    var barHeight = 19;
    var fontName = "JetBrains Mono";
    var background = new Color(0x0e, 0x48, 0x70);

    /* Config */
    context.CanMinimizeWindows = true;

    /* Gap */
    var gap = barHeight - 8;
    var gapPlugin = context.AddGap(new GapPluginConfig() { InnerGap = gap, OuterGap = gap / 2, Delta = gap / 2 });

    /* Bar */
    context.AddBar(new BarPluginConfig()
    {
        FontSize = fontSize,
        BarHeight = barHeight,
        FontName = fontName,
        DefaultWidgetBackground = background,
        LeftWidgets = () => new IBarWidget[]
        {
            new WorkspaceWidget(), new TextWidget(": "), new TitleWidget() {
                IsShortTitle = true
            }
        },
        RightWidgets = () => new IBarWidget[]
        {
            new BatteryWidget(),
            new TimeWidget(1000, "HH:mm:ss yyyy年/MMM/dd日 ddd"),
            new ActiveLayoutWidget(),
        }
    });

    /* Bar focus indicator */
    context.AddFocusIndicator();

    /* Default layouts */
    Func<ILayoutEngine[]> defaultLayouts = () => new ILayoutEngine[]
    {
        new TallLayoutEngine(),
        new VertLayoutEngine(),
        new HorzLayoutEngine(),
        new FullLayoutEngine(),
    };

    context.DefaultLayouts = defaultLayouts;

    /* Workspaces */
    // Array of workspace names and their layouts
    (string, ILayoutEngine[])[] workspaces =
    {
        ("main", defaultLayouts()),
        ("dev", new ILayoutEngine[] { new HorzLayoutEngine(), new TallLayoutEngine() }),
        ("extend", defaultLayouts()),
        ("chat", defaultLayouts()),
        ("🎶", defaultLayouts()),
        ("other", defaultLayouts()),
    };

    foreach ((string name, ILayoutEngine[] layouts) in workspaces)
    {
        context.WorkspaceContainer.CreateWorkspace(name, layouts);
    }
    
    /* Filters */
    context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("1Password.exe"));
    context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("pot.exe"));
    context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("pinentry.exe"));

    // The following filter means that Edge will now open on the correct display
    context.WindowRouter.AddFilter((window) => !window.Class.Equals("ShellTrayWnd"));

    /* Routes: 应用标题识别->自动移动到工作区 */
    context.WindowRouter.RouteProcessName("TIM", "chat");
    context.WindowRouter.RouteProcessName("微信", "chat");
    context.WindowRouter.RouteProcessName("StarUML", "extend");
    context.WindowRouter.RouteTitle("Notepad2", "dev");
    context.WindowRouter.RouteTitle("Visual Studio Code", "dev");

    /* Action menu */
    var actionMenu = context.AddActionMenu(new ActionMenuPluginConfig()
    {
        RegisterKeybind = false,
        MenuHeight = barHeight,
        FontSize = fontSize,
        FontName = fontName,
        Background = background,
    });

    /* Action menu builder */
    Func<ActionMenuItemBuilder> createActionMenuBuilder = () =>
    {
        var menuBuilder = actionMenu.Create();

        // Switch to workspace
        menuBuilder.AddMenu("switch", () =>
        {
            var workspaceMenu = actionMenu.Create();
            var monitor = context.MonitorContainer.FocusedMonitor;
            var workspaces = context.WorkspaceContainer.GetWorkspaces(monitor);

            Func<int, Action> createChildMenu = (workspaceIndex) => () =>
            {
                context.Workspaces.SwitchMonitorToWorkspace(monitor.Index, workspaceIndex);
            };

            int workspaceIndex = 0;
            foreach (var workspace in workspaces)
            {
                workspaceMenu.Add(workspace.Name, createChildMenu(workspaceIndex));
                workspaceIndex++;
            }

            return workspaceMenu;
        });

        // Move window to workspace
        menuBuilder.AddMenu("move", () =>
        {
            var moveMenu = actionMenu.Create();
            var focusedWorkspace = context.Workspaces.FocusedWorkspace;

            var workspaces = context.WorkspaceContainer.GetWorkspaces(focusedWorkspace).ToArray();
            Func<int, Action> createChildMenu = (index) => () => { context.Workspaces.MoveFocusedWindowToWorkspace(index); };

            for (int i = 0; i < workspaces.Length; i++)
            {
                moveMenu.Add(workspaces[i].Name, createChildMenu(i));
            }

            return moveMenu;
        });

        // Rename workspace
        menuBuilder.AddFreeForm("rename", (name) =>
        {
            context.Workspaces.FocusedWorkspace.Name = name;
        });

        // Create workspace
        menuBuilder.AddFreeForm("create workspace", (name) =>
        {
            context.WorkspaceContainer.CreateWorkspace(name);
        });

        // Delete focused workspace
        menuBuilder.Add("close", () =>
        {
            context.WorkspaceContainer.RemoveWorkspace(context.Workspaces.FocusedWorkspace);
        });

        // Workspacer
        menuBuilder.Add("toggle keybind helper", () => context.Keybinds.ShowKeybindDialog());
        menuBuilder.Add("toggle enabled", () => context.Enabled = !context.Enabled);
        menuBuilder.Add("restart", () => context.Restart());
        menuBuilder.Add("quit", () => context.Quit());

        return menuBuilder;
    };
    var actionMenuBuilder = createActionMenuBuilder();

    /* Keybindings */
    Action setKeybindings = () =>
    {
        KeyModifiers winShift = KeyModifiers.Alt | KeyModifiers.Shift;
        KeyModifiers winCtrl = KeyModifiers.Alt | KeyModifiers.Control;
        KeyModifiers win = KeyModifiers.Alt;

        IKeybindManager manager = context.Keybinds;

        var workspaces = context.Workspaces;

        //manager.UnsubscribeAll();
        manager.Subscribe(MouseEvent.LButtonDown, () => workspaces.SwitchFocusedMonitorToMouseLocation());

        // Left, Right keys
        //manager.Subscribe(winCtrl, Keys.Left, () => workspaces.SwitchToPreviousWorkspace(), "switch to previous workspace");
        //manager.Subscribe(winCtrl, Keys.Right, () => workspaces.SwitchToNextWorkspace(), "switch to next workspace");
        //
        //manager.Subscribe(winShift, Keys.Left, () => workspaces.MoveFocusedWindowToPreviousMonitor(), "move focused window to previous monitor");
        //manager.Subscribe(winShift, Keys.Right, () => workspaces.MoveFocusedWindowToNextMonitor(), "move focused window to next monitor");

        // H, L keys
        //manager.Subscribe(winShift, Keys.H, () => workspaces.FocusedWorkspace.ShrinkPrimaryArea(), "shrink primary area");
        //manager.Subscribe(winShift, Keys.L, () => workspaces.FocusedWorkspace.ExpandPrimaryArea(), "expand primary area");

        //manager.Subscribe(winCtrl, Keys.H, () => workspaces.FocusedWorkspace.DecrementNumberOfPrimaryWindows(), "decrement number of primary windows");
        //manager.Subscribe(winCtrl, Keys.L, () => workspaces.FocusedWorkspace.IncrementNumberOfPrimaryWindows(), "increment number of primary windows");

        // K, J keys
        //manager.Subscribe(winShift, Keys.K, () => workspaces.FocusedWorkspace.SwapFocusAndNextWindow(), "swap focus and next window");
        //manager.Subscribe(winShift, Keys.J, () => workspaces.FocusedWorkspace.SwapFocusAndPreviousWindow(), "swap focus and previous window");

        //manager.Subscribe(win, Keys.K, () => workspaces.FocusedWorkspace.FocusNextWindow(), "focus next window");
        //manager.Subscribe(win, Keys.J, () => workspaces.FocusedWorkspace.FocusPreviousWindow(), "focus previous window");

        // Add, Subtract keys
        manager.Subscribe(winCtrl, Keys.Add, () => gapPlugin.IncrementInnerGap(), "increment inner gap");
        manager.Subscribe(winCtrl, Keys.Subtract, () => gapPlugin.DecrementInnerGap(), "decrement inner gap");

        manager.Subscribe(winShift, Keys.Add, () => gapPlugin.IncrementOuterGap(), "increment outer gap");
        manager.Subscribe(winShift, Keys.Subtract, () => gapPlugin.DecrementOuterGap(), "decrement outer gap");

        // Other shortcuts
        //manager.Subscribe(winCtrl, Keys.P, () => actionMenu.ShowMenu(actionMenuBuilder), "show menu");
        //manager.Subscribe(winShift, Keys.E, () => context.Enabled = !context.Enabled, "toggle enabled/disabled");
        manager.Subscribe(winShift, Keys.Escape, () => workspaces.FocusedWorkspace.CloseFocusedWindow(), "close window");
        //manager.Subscribe(winShift, Keys.I, () => context.ToggleConsoleWindow(), "toggle console window");
    };
    setKeybindings();
});