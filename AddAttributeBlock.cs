using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;


namespace AddAttributeBlock
{
    public class AddAttributeBlock
    {
        public void AddingAttributeBlock()
        {
            // Get the current database and start a transaction
            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

            //get dimscale
            int dimscale = System.Convert.ToInt32(Application.GetSystemVariable("DIMSCALE"));

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (!acBlkTbl.Has("CircleNumber" + dimscale))
                {
                    using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                    {
                        acBlkTblRec.Name = "CircleNumber" + dimscale;

                        // Set the insertion point for the block
                        acBlkTblRec.Origin = new Point3d(0, 0, 0);

                        // Add a circle to the block
                        using (Circle acCirc = new Circle())
                        {
                            acCirc.Center = new Point3d(0, 0, 0);
                            acCirc.Radius = 2.443 * dimscale;

                            acBlkTblRec.AppendEntity(acCirc);

                            // Add an attribute definition to the block
                            using (AttributeDefinition acAttDef = new AttributeDefinition())
                            {
                                acAttDef.Position = new Point3d(0, 0, 0);
                                acAttDef.Verifiable = true;
                                acAttDef.Prompt = "Enter Number: ";
                                acAttDef.Tag = "Number#";
                                acAttDef.TextString = "00";
                                acAttDef.Height = 1.8 * dimscale;
                                acAttDef.Justify = AttachmentPoint.MiddleCenter;

                                acBlkTblRec.AppendEntity(acAttDef);

                                acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForWrite);
                                acBlkTbl.Add(acBlkTblRec);
                                acTrans.AddNewlyCreatedDBObject(acBlkTblRec, true);
                            }
                        }
                    }
                }

                // Save the new object to the database
                acTrans.Commit();

                // Dispose of the transaction
            }
        }
    }
}



