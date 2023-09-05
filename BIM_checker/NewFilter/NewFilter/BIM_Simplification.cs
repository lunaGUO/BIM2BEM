using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

///< summary>

///使用ExclusionFilter过滤元素
///< /summary>


using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;


namespace NewFilter
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    //定义一个错误处理类
    public class MyFailuresPreprocessor : IFailuresPreprocessor
    {
        private string _failureMessage;
        private bool _hasError;
        public string FailureMessage
        {
            get { return _failureMessage; }
            set { _failureMessage = value; }
        }
        public bool HasError
        {
            get { return _hasError; }
            set { _hasError = value; }
        }
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            //获取所有的失败信息
            IList<FailureMessageAccessor> failures = failuresAccessor.GetFailureMessages();
            if (failures.Count == 0)
                return FailureProcessingResult.Continue;

            foreach (FailureMessageAccessor failure in failures)
            {
                //如果是错误则尝试解决
                if (failure.GetSeverity() == FailureSeverity.Error)
                {
                    _failureMessage = failure.GetDescriptionText(); // get the failure description                
                    _hasError = true;
                    TaskDialog.Show("错误警告", "FailureProcessingResult.ProceedWithRollBack");
                    return FailureProcessingResult.ProceedWithRollBack;
                }
                //如果是警告，则禁止弹框
                if (failure.GetSeverity() == FailureSeverity.Warning)
                {
                    failuresAccessor.DeleteWarning(failure);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
    public class BIM_Simplification : IExternalCommand
    {
        public Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            UIDocument UIdoc = revit.Application.ActiveUIDocument;
            Document doc = UIdoc.Document;
            //IList<UIView> AllViews = UIdoc.GetOpenUIViews();
            //绑定revit链接！！！待完善！！！
            //ElementId LinkTypeId = new ElementId(759393);


            //查看当前视图是否为默认三维视图
            //Type type = doc.ActiveView.GetType();

            //TaskDialog.Show("View", "当前视图类型为：\n\t"+type.Name.ToString());

            //将所有元素解组
            //筛选出所有组

            List<Element> groups = new FilteredElementCollector(doc).OfClass(typeof(Group)).ToList();
            try
            {
                foreach (Element ele in groups)
                {
                    Group group = ele as Group;
                    //进行默认三维视图的删除
                    using (Transaction tran = new Transaction(doc, "ungroup"))
                    {
                        tran.Start();
                        FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                        MyFailuresPreprocessor failureProcessor = new MyFailuresPreprocessor();
                        options.SetFailuresPreprocessor(failureProcessor);
                        tran.SetFailureHandlingOptions(options);
                        ICollection<ElementId> ungroup = group.UngroupMembers();
                        tran.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ungroup", ex.Message);
            }
            TaskDialog.Show("test", "UngroupMembers Finished");
            //筛选出所有元素（除了元素编号为1的，因为必须要exclude元素，且1元素通常是项目信息元素）：用于将所有元素显示出来
            ElementId objectname = new ElementId(1);
            List<ElementId> one = new List<ElementId>();
            one.Add(objectname);
            FilteredElementCollector AllElementsCollector = new FilteredElementCollector(doc).Excluding (one);
            
            ICollection<ElementId> AllElements = AllElementsCollector.ToElementIds();


            //set active view as plan view, avoiding that active view is 3D so that can't delete {3D} 
            FilteredElementCollector ViewFamilyCollector = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType));
            FilteredElementIterator levelsIterator = (new FilteredElementCollector(doc)).OfClass(typeof(Level)).GetElementIterator();
            Level level = levelsIterator.Current as Level;
            FilteredElementCollector planViewCollector = new FilteredElementCollector(doc).OfClass(typeof(ViewPlan));
            List<ViewPlan> plans = new List<ViewPlan>();
            foreach (ViewPlan plan in planViewCollector)
            {
                if ( !(plan.GetTypeId().IntegerValue == -1) )
                {
                    plans.Add(plan);
                }
            }
            if (plans.Count() == 0)
            {
                TaskDialog.Show("Warning", "不存在平面视图，将新建平面视图");
                foreach (ViewFamilyType viewFamilyType in ViewFamilyCollector.ToList())
                {
                    if ("楼层平面".Equals(viewFamilyType.Name) || "Floor Plan".Equals(viewFamilyType.Name))
                    {
                        try
                        {
                            //进行默认三维视图的删除
                            using (Transaction tran = new Transaction(doc, "新建平面视图"))
                            {
                                tran.Start();
                                ViewPlan newViewPlan = ViewPlan.Create(doc, viewFamilyType.Id, level.Id);
                                plans.Add(newViewPlan);
                                tran.Commit();
                                TaskDialog.Show("test", "新建平面视图完成");
                            }
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("create plan view Warning", ex.Message);
                        }
                        break;
                    }
                }
                
            }
            
            foreach (ViewPlan view in plans)
            {
                UIdoc.ActiveView = view;
                ElementId planviewID = view.Id;
                string viewId = planviewID.ToString();
                //TaskDialog.Show("active view", view.Name + "\n" + viewId);
                break;
            }
            

        List<ElementId> DefaultView3D = new List<ElementId>();
        FilteredElementCollector view3DCollector = new FilteredElementCollector(doc).OfClass(typeof(View3D));
            //删除默认三维视图
            foreach (View3D view in view3DCollector)
            {
                if ((view.Name).Contains("{三维") || (view.Name).Contains("{3D"))  //如果是英文界面“{三维}”替换为“{3D}”
                {
                    DefaultView3D.Add(view.Id);
 
                }
            }
            try
            {
                //进行默认三维视图的删除
                using (Transaction tran = new Transaction(doc, "Delete 3D view"))
                {
                    tran.Start();
                    ICollection<ElementId> deletedElements = doc.Delete(DefaultView3D);
                    tran.Commit();
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Delete {三维} Warning", ex.Message);
            }
            
            //创建一个正交三维视图
            View3D NewView3D = null;
            //过滤出三维视图FamilyType
            FilteredElementCollector secondCollector = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType));
            foreach (ViewFamilyType viewFamilyType in secondCollector.ToList())
            {
                if ( "三维视图" .Equals(viewFamilyType.Name) || "3D View".Equals(viewFamilyType.Name))
                {
                    try
                    {
                        using (Transaction tran = new Transaction(doc, "创建三维视图"))
                        {
                            tran.Start("新建三维视图");
                            NewView3D = View3D.CreateIsometric(doc, viewFamilyType.Id);
                            NewView3D.Name = "轻量化三维视图";
                            tran.Commit();
                        }

                     //切换到三维视图
                     UIdoc.ActiveView = NewView3D;
                    break;
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Warning", ex.Message);
                    }

                    }
                }
                if (NewView3D == null)
                {
                    TaskDialog.Show("Warning", "创建默认三维视图失败。");
                }


            
            

            //找到所有需要排除的类别
            ElementId viewid = doc.ActiveView.Id;
            FilteredElementCollector collector1 = new FilteredElementCollector(doc,viewid);
            FilteredElementCollector All1 = new FilteredElementCollector(doc,viewid);
            FilteredElementCollector All2 = new FilteredElementCollector(doc, viewid);
            ElementCategoryFilter excludes1 = new ElementCategoryFilter(BuiltInCategory.OST_Walls);
            ElementCategoryFilter excludes2 = new ElementCategoryFilter(BuiltInCategory.OST_Windows);
            ElementCategoryFilter excludes3 = new ElementCategoryFilter(BuiltInCategory.OST_Doors);
            ElementCategoryFilter excludes4 = new ElementCategoryFilter(BuiltInCategory.OST_RoomSeparationLines);
            ElementCategoryFilter excludes5 = new ElementCategoryFilter(BuiltInCategory.OST_RoomTags);
            //ElementCategoryFilter excludes6 = new ElementCategoryFilter(BuiltInCategory.OST_MEPSpaces);
            ElementCategoryFilter excludes7 = new ElementCategoryFilter(BuiltInCategory.OST_Floors);
            ElementCategoryFilter excludes8 = new ElementCategoryFilter(BuiltInCategory.OST_Roofs);
            ElementCategoryFilter excludes9 = new ElementCategoryFilter(BuiltInCategory.OST_CurtainWallPanels);
            ElementCategoryFilter excludes10 = new ElementCategoryFilter(BuiltInCategory.OST_CurtainWallMullions);
            ElementCategoryFilter excludes11 = new ElementCategoryFilter(BuiltInCategory.OST_CurtainGridsWall);
            ElementCategoryFilter excludes12 = new ElementCategoryFilter(BuiltInCategory.OST_Cornices);
            ElementCategoryFilter excludes13 = new ElementCategoryFilter(BuiltInCategory.OST_SectionBox);
            ElementCategoryFilter excludes14 = new ElementCategoryFilter(BuiltInCategory.OST_RoomSeparationLines);
            ElementCategoryFilter excludes15 = new ElementCategoryFilter(BuiltInCategory.OST_CurtaSystemFaceManager);
            ElementCategoryFilter excludes16 = new ElementCategoryFilter(BuiltInCategory.OST_Cameras);
            ElementCategoryFilter excludes17 = new ElementCategoryFilter(BuiltInCategory.OST_CurtainGridsRoof);
            ElementCategoryFilter excludes18 = new ElementCategoryFilter(BuiltInCategory.OST_CurtainWallMullionsCut);
            ElementCategoryFilter excludes19 = new ElementCategoryFilter(BuiltInCategory.OST_CurtainGrids);
            ElementCategoryFilter excludes20 = new ElementCategoryFilter(BuiltInCategory.OST_CurtainGridsSystem);
            ElementCategoryFilter excludes21 = new ElementCategoryFilter(BuiltInCategory.OST_CurtaSystem);
            ElementCategoryFilter excludes22 = new ElementCategoryFilter(BuiltInCategory.OST_CurtainWallMullionsCut);
            ElementCategoryFilter excludes23 = new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming);
            ElementCategoryFilter excludes24 = new ElementCategoryFilter(BuiltInCategory.OST_IOSModelGroups);
            ElementCategoryFilter excludes25 = new ElementCategoryFilter(BuiltInCategory.OST_RvtLinks);
            ElementCategoryFilter excludes26 = new ElementCategoryFilter(BuiltInCategory.OST_ShaftOpening);
            ElementCategoryFilter excludes27 = new ElementCategoryFilter(BuiltInCategory.OST_WallAnalytical);
            ElementCategoryFilter excludes28 = new ElementCategoryFilter(BuiltInCategory.OST_FloorAnalytical);
            ElementCategoryFilter excludes29 = new ElementCategoryFilter(BuiltInCategory.OST_Columns); 
            ElementCategoryFilter excludes30 = new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns);
            // 先把columns保留下来，后面再逐一做处理

            List<ElementFilter> filterSet = new List<ElementFilter>();
            filterSet.Add(excludes1);
            filterSet.Add(excludes2);
            filterSet.Add(excludes3);
            filterSet.Add(excludes4);
            filterSet.Add(excludes5);
            //filterSet.Add(excludes6);
            filterSet.Add(excludes7);
            filterSet.Add(excludes8);
            filterSet.Add(excludes9); 
            filterSet.Add(excludes10);
            filterSet.Add(excludes11);
            filterSet.Add(excludes12);
            filterSet.Add(excludes13);
            filterSet.Add(excludes14);
            filterSet.Add(excludes15);
            filterSet.Add(excludes16);
            filterSet.Add(excludes17);
            filterSet.Add(excludes18);
            filterSet.Add(excludes19);
            filterSet.Add(excludes20);
            filterSet.Add(excludes21);
            filterSet.Add(excludes22);
            filterSet.Add(excludes23);
            filterSet.Add(excludes24);
            filterSet.Add(excludes25);
            filterSet.Add(excludes26);
            filterSet.Add(excludes27);
            filterSet.Add(excludes28);
            filterSet.Add(excludes29);
            filterSet.Add(excludes30);
            LogicalOrFilter AllExcludes = new LogicalOrFilter(filterSet);
            ICollection<ElementId> excludesIds = collector1.WherePasses(AllExcludes).ToElementIds();
            //TaskDialog.Show("Revit", excludesIds.Count().ToString());


                       //collector1.OfCategory(BuiltInCategory.OST_Walls).OfCategory(BuiltInCategory.OST_Doors).OfCategory(BuiltInCategory.OST_Windows).OfCategory(BuiltInCategory.OST_RoomSeparationLines).OfCategory(BuiltInCategory.OST_RoomTags).OfCategory(BuiltInCategory.OST_MEPSpaces).OfCategory(BuiltInCategory.OST_Floors).OfCategory(BuiltInCategory.OST_Roofs);
                       //ICollection<ElementId> excludes = collector.ToElementIds();

            //创建一个排除Category的过滤器
            FilteredElementCollector collector2 = new FilteredElementCollector(doc,viewid);
            ExclusionFilter filter = new ExclusionFilter(excludesIds);

            //ICollection<ElementId> founds = collector.WhereElementIsElementType().

            ICollection<ElementId> deleteIds = collector2.WherePasses(filter).ToElementIds();
            //List<ElementId> elementsToDelete = new List<ElementId>(deleteIds);
            String prompt = "exclude filter删除的元素: ";
            foreach (ElementId id in deleteIds)
            {
                prompt += "\n\t" + id.IntegerValue;
            }
            TaskDialog.Show("To Delete", prompt);
            TaskDialog.Show("To Delete", deleteIds.Count().ToString());


            try
            {
                //进行元素的删除
                using (Transaction tran = new Transaction(doc, "Delete the selected elements."))
                {
                    tran.Start();
                    ICollection<ElementId> deletedElements = doc.Delete(deleteIds);
                    tran.Commit();
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Exclude Delete Warning", deleteIds.ToString()+"\n"+ ex.Message);
            }


            //删除视图中没有的但需要删除的元素
            ElementCategoryFilter SpacesFilter = new ElementCategoryFilter(BuiltInCategory.OST_MEPSpaces);
            ElementCategoryFilter HVACZoneFilter = new ElementCategoryFilter(BuiltInCategory.OST_HVAC_Zones);
            ICollection<ElementId> Spaces = AllElementsCollector.WherePasses(SpacesFilter).ToElementIds();
            ICollection<ElementId> HVACZone = AllElementsCollector.WherePasses(HVACZoneFilter).ToElementIds();
            //int count_test = Spaces.Count + HVACZone.Count;
            //TaskDialog.Show("Space和Zone个数", count_test.ToString());
            try
            {
                //进行元素的删除
                using (Transaction tran = new Transaction(doc, "Delete the selected elements."))
                {
                    tran.Start();
                    ICollection<ElementId> deletedElements1 = doc.Delete(Spaces);
                    ICollection<ElementId> deletedElements2 = doc.Delete(HVACZone);
                    tran.Commit();
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Delete Warning", ex.Message);
            }


            //单独删除无法直接用category保留下来的元素
            ElementCategoryFilter corniceFilter = new ElementCategoryFilter(BuiltInCategory.OST_Cornices);
            ElementCategoryFilter framingFilter = new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming); 
            ICollection<ElementId> cornice = All1.WherePasses(corniceFilter).ToElementIds();
            ICollection<ElementId> framing = All2.WherePasses(framingFilter).ToElementIds();
            //TaskDialog.Show("test category framing", framing.Count.ToString());
            //foreach (ElementId elementId in cornice)
            List<ElementId> deleteOther = new List<ElementId>();
            for(int i = 0; i < cornice.Count; i++)
            {
                ElementId elementId = cornice.ElementAt(i);
                Element decorate = doc.GetElement(elementId);
                string name = decorate.Category.Name;
                //TaskDialog.Show("category name",name);
                if (name == "墙饰条")
                {
                    deleteOther.Add(elementId);
                }
            }
            for (int i = 0; i < framing.Count; i++)
            {
                ElementId elementId = framing.ElementAt(i);
                Element bridge = doc.GetElement(elementId);
                string name = bridge.Category.Name;
                //TaskDialog.Show("category name", name);
                if (name == "结构框架")
                {
                    deleteOther.Add(elementId);
                }
            }
            

            for (int i = 0; i < deleteOther.Count; i++)
            {
                try
                {
                    //进行元素的删除
                    using (Transaction tran = new Transaction(doc, "Delete the selected elements."))
                    {
                        tran.Start();
                        ICollection<ElementId> DeletedElements = doc.Delete(deleteOther[i]);
                        tran.Commit();
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("other Delete Warning", deleteOther[i].ToString ()+"\n"+ex.Message);
                }
            }
           
            //将所有的墙的房间边界属性都设置为true
            FilteredElementCollector All3 = new FilteredElementCollector(doc);
            ElementCategoryFilter wallFilter = new ElementCategoryFilter(BuiltInCategory.OST_Walls);
            ICollection<ElementId> walls = All3.OfClass(typeof (Wall)).WherePasses(wallFilter).ToElementIds();
            foreach (ElementId elementId in walls)
            {
                Wall wall = doc.GetElement(elementId) as Wall;
                Parameter roomBounding = wall.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);
                try
                {
                    //修改墙的房间边界参数
                    using (Transaction tran = new Transaction(doc, "WALL_ATTR_ROOM_BOUNDING set to true"))
                    {
                        tran.Start();
                        roomBounding.Set(1);
                        tran.Commit();
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Room bounding Warning", ex.Message);
                }
            }
            

            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }
}
/*
          try
          {
              //进行默认三维视图的删除
              using (Transaction tran = new Transaction(doc, "绑定revit链接"))
              {
                  tran.Start();
                  RevitLinkInstance.Create(doc, LinkTypeId);
                  tran.Commit();
                  TaskDialog.Show("test", "绑定链接完成");
              }
          }
          catch (Exception ex)
          {
              TaskDialog.Show("binding RvtLink Warning", ex.Message);
          }
          */


/*        
       string ViewName = doc.ActiveView.Name;
       if (ViewName.Contains("{三维"))
       {
           TaskDialog.Show("View", "当前视图为{三维}视图");

           //设置图形可见性
           using (Transaction tran = new Transaction(doc, "UnhideElements"))
           {
               tran.Start();
               doc.ActiveView.UnhideElements(AllElements);
               doc.ActiveView.Name = "轻量化三维视图";
               tran.Commit();
           }


       }

       else
       {
           View3D view3D = null;

           //查看当前文件有没有默认3D视图
           FilteredElementCollector view3DCollector = new FilteredElementCollector(doc).OfClass(typeof(View3D));
           //如果存在默认三维视图则将默认三维视图设为当前视图并设置所有图元可见
           foreach (View3D view in view3DCollector)
           {
               if ((view.Name).Contains("{三维"))  //如果是英文界面“{三维}”替换为“{3D}”
               {
                   view3D = view;

                   TaskDialog.Show("View", "存在默认{三维}视图: "+view.Name  );
                   UIdoc.ActiveView = view;

                   //设置图形可见性
                   using (Transaction tran = new Transaction(doc, "UnhideElements"))
                   {
                       tran.Start();
                       doc.ActiveView.UnhideElements(AllElements);
                       doc.ActiveView.Name = "轻量化三维视图";
                       tran.Commit();
                   }
                   break;
               }

           }

           //如果没有找到默认三维视图，则创建一个正交三维视图
           if (view3D == null)
           {


               //过滤出三维视图FamilyType
               FilteredElementCollector secondCollector = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType));
               foreach (ViewFamilyType viewFamilyType in secondCollector.ToList())
               {

                   if ("3D View".Equals(viewFamilyType.Name))
                   {
                       try
                       {
                           using (Transaction tran = new Transaction(doc, "创建三维视图"))
                           {
                               tran.Start("新建三维视图");
                               view3D = View3D.CreateIsometric(doc, viewFamilyType.Id);
                               view3D.Name = "轻量化三维视图";
                               tran.Commit();
                           }

                           //切换到三维视图
                           UIdoc.ActiveView = view3D;

                           break;
                       }
                       catch (Exception ex)
                       {
                           TaskDialog.Show("Warning", ex.Message);
                       }

                   }
               }
               if (view3D == null)
               {
                   TaskDialog.Show("Warning", "创建默认三维视图失败。");
               }
            }        
       }
*/

