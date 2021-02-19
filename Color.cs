using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

[assembly: CommandClass(typeof(Color.PickFirst_1)), CommandClass(typeof(Color.PickFirst_2)),
    CommandClass(typeof(Color.PickFirst_3)), CommandClass(typeof(Color.PickFirst_4)),
    CommandClass(typeof(Color.PickFirst_5)), CommandClass(typeof(Color.PickFirst_6)),
    CommandClass(typeof(Color.PickFirst_7))]

namespace Color
{
    public class PickFirst_1
    {

        [CommandMethod("11", CommandFlags.UsePickSet | CommandFlags.Modal)]
        public static void CheckForPickfirstSelection()
        {
            //Get the current document and databse
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            pso.SinglePickInSpace = true;

            //If the prompt status is OK, objects were selected before
            //the command was started
            //Start Transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //Get the PickFirst selection set
                PromptSelectionResult psr = ed.GetSelection(pso);

                SelectionSet sset;

                if (psr.Status == PromptStatus.OK)
                {
                    sset = psr.Value;

                    foreach (SelectedObject ssobj in sset)
                    {
                        //Check to make sure a valid SelectedObject object was returned
                        if (ssobj != null)
                        {
                            //Open the selected object for write
                            Entity ent = trans.GetObject(ssobj.ObjectId,
                                                            OpenMode.ForWrite) as Entity;

                            if (ent != null)
                            {
                                //Change the obejct's color to green
                                ent.ColorIndex = 1;
                            }
                        }
                    }

                    //Save the new object to the database
                    trans.Commit();
                }
            }
        }
    }
    public class PickFirst_2
    {

        [CommandMethod("22", CommandFlags.UsePickSet | CommandFlags.Modal)]
        public static void CheckForPickfirstSelection()
        {
            //Get the current document and databse
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            pso.SinglePickInSpace = true;

            //If the prompt status is OK, objects were selected before
            //the command was started
            //Start Transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //Get the PickFirst selection set
                PromptSelectionResult psr = ed.GetSelection(pso);

                SelectionSet sset;

                if (psr.Status == PromptStatus.OK)
                {
                    sset = psr.Value;

                    foreach (SelectedObject ssobj in sset)
                    {
                        //Check to make sure a valid SelectedObject object was returned
                        if (ssobj != null)
                        {
                            //Open the selected object for write
                            Entity ent = trans.GetObject(ssobj.ObjectId,
                                                            OpenMode.ForWrite) as Entity;

                            if (ent != null)
                            {
                                //Change the obejct's color to green
                                ent.ColorIndex = 2;
                            }
                        }
                    }

                    //Save the new object to the database
                    trans.Commit();
                }
            }
        }
    }
    public class PickFirst_3
    {

        [CommandMethod("33", CommandFlags.UsePickSet | CommandFlags.Modal)]
        public static void CheckForPickfirstSelection()
        {
            //Get the current document and databse
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            pso.SinglePickInSpace = true;

            //If the prompt status is OK, objects were selected before
            //the command was started
            //Start Transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //Get the PickFirst selection set
                PromptSelectionResult psr = ed.GetSelection(pso);

                SelectionSet sset;

                if (psr.Status == PromptStatus.OK)
                {
                    sset = psr.Value;

                    foreach (SelectedObject ssobj in sset)
                    {
                        //Check to make sure a valid SelectedObject object was returned
                        if (ssobj != null)
                        {
                            //Open the selected object for write
                            Entity ent = trans.GetObject(ssobj.ObjectId,
                                                            OpenMode.ForWrite) as Entity;

                            if (ent != null)
                            {
                                //Change the obejct's color to green
                                ent.ColorIndex = 3;
                            }
                        }
                    }

                    //Save the new object to the database
                    trans.Commit();
                }
            }
        }
    }
    public class PickFirst_4
    {

        [CommandMethod("44", CommandFlags.UsePickSet | CommandFlags.Modal)]
        public static void CheckForPickfirstSelection()
        {
            //Get the current document and databse
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            pso.SinglePickInSpace = true;

            //If the prompt status is OK, objects were selected before
            //the command was started
            //Start Transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //Get the PickFirst selection set
                PromptSelectionResult psr = ed.GetSelection(pso);

                SelectionSet sset;

                if (psr.Status == PromptStatus.OK)
                {
                    sset = psr.Value;

                    foreach (SelectedObject ssobj in sset)
                    {
                        //Check to make sure a valid SelectedObject object was returned
                        if (ssobj != null)
                        {
                            //Open the selected object for write
                            Entity ent = trans.GetObject(ssobj.ObjectId,
                                                            OpenMode.ForWrite) as Entity;

                            if (ent != null)
                            {
                                //Change the obejct's color to green
                                ent.ColorIndex = 4;
                            }
                        }
                    }

                    //Save the new object to the database
                    trans.Commit();
                }
            }
        }
    }
    public class PickFirst_5
    {

        [CommandMethod("55", CommandFlags.UsePickSet | CommandFlags.Modal)]
        public static void CheckForPickfirstSelection()
        {
            //Get the current document and databse
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            pso.SinglePickInSpace = true;

            //If the prompt status is OK, objects were selected before
            //the command was started
            //Start Transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //Get the PickFirst selection set
                PromptSelectionResult psr = ed.GetSelection(pso);

                SelectionSet sset;

                if (psr.Status == PromptStatus.OK)
                {
                    sset = psr.Value;

                    foreach (SelectedObject ssobj in sset)
                    {
                        //Check to make sure a valid SelectedObject object was returned
                        if (ssobj != null)
                        {
                            //Open the selected object for write
                            Entity ent = trans.GetObject(ssobj.ObjectId,
                                                            OpenMode.ForWrite) as Entity;

                            if (ent != null)
                            {
                                //Change the obejct's color to green
                                ent.ColorIndex = 5;
                            }
                        }
                    }

                    //Save the new object to the database
                    trans.Commit();
                }
            }
        }
    }
    public class PickFirst_6
    {

        [CommandMethod("66", CommandFlags.UsePickSet | CommandFlags.Modal)]
        public static void CheckForPickfirstSelection()
        {
            //Get the current document and databse
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            pso.SinglePickInSpace = true;

            //If the prompt status is OK, objects were selected before
            //the command was started
            //Start Transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //Get the PickFirst selection set
                PromptSelectionResult psr = ed.GetSelection(pso);

                SelectionSet sset;

                if (psr.Status == PromptStatus.OK)
                {
                    sset = psr.Value;

                    foreach (SelectedObject ssobj in sset)
                    {
                        //Check to make sure a valid SelectedObject object was returned
                        if (ssobj != null)
                        {
                            //Open the selected object for write
                            Entity ent = trans.GetObject(ssobj.ObjectId,
                                                            OpenMode.ForWrite) as Entity;

                            if (ent != null)
                            {
                                //Change the obejct's color to green
                                ent.ColorIndex = 6;
                            }
                        }
                    }

                    //Save the new object to the database
                    trans.Commit();
                }
            }
        }
    }
    public class PickFirst_7
    {

        [CommandMethod("77", CommandFlags.UsePickSet | CommandFlags.Modal)]
        public static void CheckForPickfirstSelection()
        {
            //Get the current document and databse
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            pso.SinglePickInSpace = true;

            //If the prompt status is OK, objects were selected before
            //the command was started
            //Start Transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //Get the PickFirst selection set
                PromptSelectionResult psr = ed.GetSelection(pso);

                SelectionSet sset;

                if (psr.Status == PromptStatus.OK)
                {
                    sset = psr.Value;

                    foreach (SelectedObject ssobj in sset)
                    {
                        //Check to make sure a valid SelectedObject object was returned
                        if (ssobj != null)
                        {
                            //Open the selected object for write
                            Entity ent = trans.GetObject(ssobj.ObjectId,
                                                            OpenMode.ForWrite) as Entity;

                            if (ent != null)
                            {
                                //Change the obejct's color to green
                                ent.ColorIndex = 7;
                            }
                        }
                    }

                    //Save the new object to the database
                    trans.Commit();
                }
            }
        }
    }
}