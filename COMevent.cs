using System.IO;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using AutoCAD;

[assembly : CommandClass(typeof(Command_Setting.COMevent))]

namespace Command_Setting
{
    class COMevent
    {
        // Global variable for AddCOMEvent and RemoveCOMEvent commands
        AcadApplication acAppCom;

        [CommandMethod("AddCOMEvent")]
        public void AddCOMEvent()
        {
            // Set the global variable to hold a reference to the application and
            // register the BeginFileDrop COM event
            acAppCom = Application.AcadApplication as AcadApplication;
            acAppCom.BeginFileDrop += new _DAcadApplicationEvents_BeginFileDropEventHandler(appComBeginFileDrop);
        }

        [CommandMethod("RemoveCOMEvent")]
        public void RemoveCOMEvent()
        {
            // Unregister the COM event handle
            acAppCom.BeginFileDrop -= new _DAcadApplicationEvents_BeginFileDropEventHandler(appComBeginFileDrop);
            acAppCom = null;
        }

        public void appComBeginFileDrop(string strFileName, ref bool Cancel)
        {
            // Display a message box prompting to continue inserting the DWG file
            if (System.Windows.Forms.MessageBox.Show("AutoCAD is about to load " + strFileName + "\nDo you want to continue loading this file?", "DWG File Dropped", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
            {
                Cancel = true;
            }
            else
            {
                DocumentCollection acDocMgr = Application.DocumentManager;
                if (File.Exists(strFileName))
                {
                    acDocMgr.Open(strFileName, false);
                }
                else
                {
                    acDocMgr.MdiActiveDocument.Editor.WriteMessage("File " + strFileName + " does not exist.");
                }
            }
        }
    }
}

 
