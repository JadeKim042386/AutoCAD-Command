using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;

[assembly :CommandClass(typeof(BeginQuitEvent.BeginQuitEvent))]

namespace BeginQuitEvent
{
    class BeginQuitEvent
    {
        [CommandMethod("AddAppEvent")]
        public void AddAppEvent()
        {
            Application.SystemVariableChanged += new SystemVariableChangedEventHandler(appSysVarChanged);
        }

        [CommandMethod("RemoveAppEvent")]
        public void RemoveAppEvent()
        {
            Application.SystemVariableChanged -= new SystemVariableChangedEventHandler(appSysVarChanged);
        }

        public void appSysVarChanged(object senderObj, SystemVariableChangedEventArgs sysVarChEvtArgs)
        {
            object oVal = Application.GetSystemVariable(sysVarChEvtArgs.Name);

            // Display a message box with the system variable name and the new value
            Application.ShowAlertDialog(sysVarChEvtArgs.Name + " was changed." + "\nNew value: " + oVal.ToString());
        }
    }
}

