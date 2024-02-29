using System;
using System.Data;
using System.Web.UI.WebControls;

using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.Helpers;
using CMS.SiteProvider;
using CMS.Taxonomy;
using CMS.UIControls;


[EditedObject(DocumentTypeInfo.OBJECT_TYPE_DOCUMENTTYPE, "objectid")]
public partial class CMSModules_DocumentTypes_Pages_Development_DocumentType_Edit_AllowedCategories : GlobalAdminPage
{
    #region "Variables"

    private string currentValues = string.Empty;
    private int selectedSiteId = 0;

    #endregion


    #region "Properties"

    /// <summary>
    /// Current document type object.
    /// </summary>
    private DataClassInfo DocumentType
    {
        get
        {
            return (DataClassInfo)EditedObject;
        }
    }

    #endregion


    #region "Methods"

    protected void Page_Load(object sender, EventArgs e)
    {
        CurrentMaster.DisplaySiteSelectorPanel = true;

        selectedSiteId = !RequestHelper.IsPostBack() ? SiteContext.CurrentSiteID : ValidationHelper.GetInteger(selectSite.Value, UniSelector.US_ALL_RECORDS);

        selectSite.PostbackOnDropDownChange = true;
        selectSite.UniSelector.OnSelectionChanged += DropDownSingleSelect_SelectedIndexChanged;
        selectSite.UniSelector.WhereCondition = "SiteID IN (SELECT SiteID FROM CMS_ClassSite WHERE ClassID = " + DocumentType.ClassID + ")";
        selectSite.SiteID = selectedSiteId;
        selectSite.Reload(false);

        // Ensure correct site selection (page type isn't assigned to current site fix)
        selectedSiteId = ValidationHelper.GetInteger(selectSite.Value, UniSelector.US_ALL_RECORDS);

        usCategories.SelectItemPageUrl = "~/CMSModules/Categories/Dialogs/CategorySelection.aspx";
        usCategories.DisabledItems = "personal";
        usCategories.SetValue("SiteID", selectedSiteId);

        // Load current values for the current page type and selected site
        DataSet ds = GetAssignedAllowedCategories(selectedSiteId, DocumentType.ClassID);
        currentValues = TextHelper.Join(";", DataHelper.GetStringValues(ds.Tables[0], "CategoryID"));

        if (!RequestHelper.IsPostBack())
        {
            usCategories.Value = currentValues;
        }

        if (selectedSiteId > 0)
        {
            usCategories.WhereCondition = "CategorySiteID IS NULL OR CategorySiteID = " + selectedSiteId;
            usCategories.ButtonNewEnabled = true;
        }
        else
        {
            usCategories.ButtonNewEnabled = false;
            usCategories.UniGrid.OnBeforeDataReload += UniGrid_OnBeforeDataReload;
        }

        usCategories.DisabledAddButtonExplanationText = GetString("documenttype.allowedcategories.choosesite");
        usCategories.OnSelectionChanged += usCategories_OnSelectionChanged;
    }

    
    protected void UniGrid_OnBeforeDataReload()
    {
        if ((usCategories.UniGrid.GridView.Columns.Count > 0) && !usCategories.UniGrid.NamedColumns.ContainsKey("SiteName"))
        {
            // Add additional site column
            ExtendedBoundField field = new ExtendedBoundField();
            field.HeaderText = GetString("general.sitename");
            field.DataField = "CategorySiteID";
            field.ExternalSourceName = "#sitename";
            field.ItemStyle.Wrap = false;
            field.HeaderStyle.Width = new Unit("100%");
            field.OnExternalDataBound += usCategories.UniGrid.RaiseExternalDataBound;

            usCategories.UniGrid.GridView.Columns.Add(field);
            usCategories.UniGrid.NamedColumns.Add("SiteName", field);

            // Clear width for item name column
            usCategories.UniGrid.GridView.Columns[1].HeaderStyle.Width = new Unit();
        }
    }


    protected void DropDownSingleSelect_SelectedIndexChanged(object sender, EventArgs e)
    {
        usCategories.Value = currentValues;
        usCategories.Reload(true);
    }


    protected void usCategories_OnSelectionChanged(object sender, EventArgs e)
    {
        // Remove old items
        string newValues = ValidationHelper.GetString(usCategories.Value, null);
        string items = DataHelper.GetNewItemsInList(newValues, currentValues);
        HandleAllowedCategoriesRemoval(items);

        // Add new items
        items = DataHelper.GetNewItemsInList(currentValues, newValues);
        HandleAllowedCategoriesAddition(items);
    }


    private DataSet GetAssignedAllowedCategories(int siteId, int pageTypeId)
    {
        var query = CategoryClassInfo.Provider.Get()
            .Column($"{CategoryClassInfo.TYPEINFO.ClassStructureInfo.TableName}.{nameof(CategoryClassInfo.CategoryID)}")
            .Source(s => s.InnerJoin<CategoryInfo>(nameof(CategoryClassInfo.CategoryID), nameof(CategoryInfo.CategoryID)))
            .WhereEquals(nameof(CategoryClassInfo.ClassID), pageTypeId);

        if (siteId > 0)
        {
            var nestedWhere = new WhereCondition()
                .WhereEquals(nameof(CategoryInfo.CategorySiteID), siteId)
                .Or()
                .WhereNull(nameof(CategoryInfo.CategorySiteID));
            query.Where(nestedWhere);
        }

        return query;
    }


    private void HandleAllowedCategoriesAddition(string items)
    {
        if (String.IsNullOrEmpty(items))
        {
            return;
        }

        string[] newItems = items.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (newItems is null)
        {
            return;
        }

        // Add all new categories to page type
        foreach (string item in newItems)
        {
            int newCategoryId = ValidationHelper.GetInteger(item, 0);
            var categoryInfo = CategoryInfo.Provider.Get(newCategoryId);

            // if category did not have any bindings before, update the flag
            if (categoryInfo.CategoryAllowAllTypes)
            {
                categoryInfo.CategoryAllowAllTypes = false;
                categoryInfo.Update();
            }

            CategoryClassInfo.Provider.Add(newCategoryId, DocumentType.ClassID);
        }
    }


    private void HandleAllowedCategoriesRemoval(string items)
    {
        if (String.IsNullOrEmpty(items))
        {
            return;
        }

        string[] newItems = items.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (newItems is null)
        {
            return;
        }

        // Remove allowed categories from page type
        foreach (string item in newItems)
        {
            var removedCategoryId = ValidationHelper.GetInteger(item, 0);
            CategoryClassInfo.Provider.Remove(removedCategoryId, DocumentType.ClassID);

            // if category no longer has any bindings, update the flag to allow it for all page types
            var removedCategoryStillHasBindings = CategoryClassInfo.Provider.Get()
                .Column(nameof(CategoryClassInfo.ClassID))
                .TopN(1)
                .WhereEquals(nameof(CategoryClassInfo.CategoryID), removedCategoryId)
                .GetScalarResult(0) > 0;

            if (!removedCategoryStillHasBindings)
            {
                var removedCategory = CategoryInfo.Provider.Get(removedCategoryId);
                removedCategory.CategoryAllowAllTypes = true;
                removedCategory.Update();
            }
        }
    }

    #endregion
}
