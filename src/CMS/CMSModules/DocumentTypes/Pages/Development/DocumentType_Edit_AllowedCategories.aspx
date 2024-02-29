<%@ Page Language="C#" AutoEventWireup="true"
    Inherits="CMSModules_DocumentTypes_Pages_Development_DocumentType_Edit_AllowedCategories"
    MasterPageFile="~/CMSMasterPages/UI/SimplePage.master" Title="Page type edit - Allowed categories"
    Theme="Default"  Codebehind="DocumentType_Edit_AllowedCategories.aspx.cs" %>

<%@ Register Src="~/CMSFormControls/Sites/SiteSelector.ascx" TagName="SiteSelector" TagPrefix="cms" %>
<%@ Register Src="~/CMSAdminControls/UI/UniSelector/UniSelector.ascx" TagName="UniSelector" TagPrefix="cms" %>
<asp:Content ID="cntSiteSelector" ContentPlaceHolderID="plcSiteSelector" runat="server">
    <asp:Panel ID="pnlSite" runat="server">
        <div class="form-horizontal form-filter scopes-filter">
            <div class="form-group">
                <div class="filter-form-label-cell">
                    <cms:LocalizedLabel CssClass="control-label" runat="server" ID="lblSite" DisplayColon="true" ResourceString="general.site" EnableViewState="false" />
                </div>
                <div class="filter-form-value-cell-wide">
                    <cms:SiteSelector runat="server" ID="selectSite" IsLiveSite="false" />
                </div>
            </div>
        </div>
    </asp:Panel>
</asp:Content>
<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="plcContent">
    <cms:LocalizedHeading ID="headTitle" Level="4" runat="server" CssClass="listing-title" EnableViewState="false" ResourceString="documenttype.allowedcategories.heading" DisplayColon="true" />
    <cms:UniSelector ID="usCategories" runat="server" ReturnColumnName="CategoryID"
        ObjectType="cms.categorylist" ResourcePrefix="categoryselector" OrderBy="CategoryNamePath"
        AdditionalColumns="CategorySiteID" SelectionMode="Multiple"
        AllowEmpty="false" IsLiveSite="false" DialogWindowWidth="800" />
</asp:Content>
