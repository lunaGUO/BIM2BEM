using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace column
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Class1 : IExternalCommand
    {
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            Document doc = revit.Application.ActiveUIDocument.Document;
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            //把柱子筛选出来，柱子可能有建筑柱，也可能有结构柱
            ElementCategoryFilter Fcolumns = new ElementCategoryFilter(BuiltInCategory.OST_Columns);
            ElementCategoryFilter FScolumns = new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns);
            ICollection<ElementId> columns = collector.WherePasses(Fcolumns).ToElementIds();
            ICollection<ElementId> Scolumns = collector.WherePasses(FScolumns).ToElementIds();
            List<ElementId> co = new List<ElementId>();
            foreach (ElementId el in columns)
            {
                co.Add(el);
            }
            foreach (ElementId el in Scolumns)
            {
                co.Add(el);
            }


            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }
}
