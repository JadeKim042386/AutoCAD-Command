using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

[assembly : CommandClass(typeof(Object_Explorer_Template.Object_Explorer_Template))]

namespace Object_Explorer_Template
{
    class Object_Explorer_Template
    {
        [CommandMethod("TEST2")]
        public static void Find_obj()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using(Transaction tr = db.TransactionManager.StartTransaction())
            {
                var btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false) as BlockTableRecord;

                foreach(ObjectId objid in btr)
                {
                    if (objid.ObjectClass.DxfName == "MULTILEADER")
                    {
                        MLeader cur_leader = tr.GetObject(objid, OpenMode.ForRead) as MLeader;

                        ResultBuffer rb = cur_leader.XData;

                        if (rb == null)
                        {
                            ed.WriteMessage("\nEntity does not have XData attached.");
                        }
                        else
                        {
                            foreach (TypedValue tv in rb)
                            {
                                ed.WriteMessage("\nTypedValue {0} - type: {1}, value: {2}", tv.TypeCode, tv.Value);
                            }

                            rb.Dispose();
                        }
                    }
                }
            }
        }
    }
}
