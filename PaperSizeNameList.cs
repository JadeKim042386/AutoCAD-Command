using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

[assembly : CommandClass(typeof(Command_Setting.PaperSizeNameList))]

namespace Command_Setting
{
    class PaperSizeNameList
    {
        // Lists the available local media names for a specified plot configuration (PC3) file
        [CommandMethod("MediaNameList")]
        public static void PlotterLocalMediaNameList()
        {
            // Get the current document and database, and start a transaction
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            using (PlotSettings plSet = new PlotSettings(true))
            {
                PlotSettingsValidator acPlSetVdr = PlotSettingsValidator.Current;

                // Set the Plotter and page size
                acPlSetVdr.SetPlotConfigurationName(plSet, "Adobe PDF", "A3");

                acDoc.Editor.WriteMessage("\nCanonical and Local media names: ");

                int cnt = 0;

                foreach (string mediaName in acPlSetVdr.GetCanonicalMediaNameList(plSet))
                {
                    // Output the names of the available media for the specified device
                    acDoc.Editor.WriteMessage("\n  " + mediaName + " | " +
                                              acPlSetVdr.GetLocaleMediaName(plSet, cnt));

                    cnt = cnt + 1;
                }
            }
        }
    }
}
