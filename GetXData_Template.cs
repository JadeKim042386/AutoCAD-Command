using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace GetXData_Template
{
    class GetXData_Template
    {
        static public void GetXData()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            Editor ed = doc.Editor;

            PromptEntityOptions opt = new PromptEntityOptions("\nSelect entity: ");

            PromptEntityResult res = ed.GetEntity(opt);

            if (res.Status == PromptStatus.OK)
            {
                using (Transaction tr = doc.TransactionManager.StartTransaction())
                {
                    DBObject obj = tr.GetObject(res.ObjectId, OpenMode.ForRead);

                    ResultBuffer rb = obj.XData;

                    if (rb == null)
                    {
                        ed.WriteMessage("\nEntity does not have XData attached.");
                    }
                    else
                    {
                        int n = 0;

                        foreach (TypedValue tv in rb)
                        {
                            ed.WriteMessage("\nTypedValue {0} - type: {1}, value: {2}", n++, tv.TypeCode, tv.Value);
                        }

                        rb.Dispose();
                    }
                }
            }
        }
    }
}
