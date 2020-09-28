using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Utility;
using ColecticaSdkMvc.Models;
using ColecticaSdkMvc.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Configuration;
using System.Net.Http;

namespace EquivalencesTest.Controllers
{
    public class DatasetController : Controller
    {
   
        // GET: Dataset
        public ActionResult Index()
        {
            DataSetModel model = new DataSetModel();
            model.DataSet = new Collection<DataSetItem>();
            model.ItemTypes = new List<Item>();
            model.Urns = new List<string>();
            return View(model);
        }

        [HttpPost]
        public ActionResult Index(DataSetModel model, string command, HttpPostedFileBase postedFile)
        {
            if (postedFile != null)
            {
                try
                {
                    string fileExtension = Path.GetExtension(postedFile.FileName);
                    if (fileExtension != ".csv")
                    {
                        return View(model);
                    }
                    model.Urns = new List<string>();
                    using (var sreader = new StreamReader(postedFile.InputStream))
                    {
                        while (!sreader.EndOfStream)
                        {
                            // string[] rows = sreader.ReadLine().Split(',');   
                            string row = sreader.ReadLine();
                            model.Urns.Add(row.ToString());

                            // model.Urns.Add(rows[0].ToString());
                        }
                    }                   
                    return View(model);
                }
                catch (Exception ex)
                {
                    ViewBag.Message = ex.Message;
                }
            }
            switch (command)
            {
                case "Search":
                    var identifier = model.Urn.Substring(model.Urn.IndexOf("/") + 1, model.Urn.Length - model.Urn.IndexOf("/") - 1);
                    var agency = model.Urn.Substring(0, model.Urn.IndexOf("/"));
                    model = ProcessDataSet(model, agency, new Guid(identifier));
                    model.Urns = new List<string>();
                    var identifiertriple = new IdentifierTriple(new Guid(identifier), 1, agency);
                    List<TreeViewNode> nodes = new List<TreeViewNode>();
                    nodes = BuildTree(identifiertriple, nodes);
                    ViewBag.Json = (new JavaScriptSerializer()).Serialize(nodes);
                    return View(model);
                case "Deprecate":
                    DeprecateItems(model);
                    model.DataSet = new Collection<DataSetItem>();
                    model.ItemTypes = new List<Item>();
                    break;
                case "Deprecate All":
                    model = ProcessAllData(model);
                    break;
                case "Get Set":
                    var identifier1 = model.Urn.Substring(model.Urn.IndexOf("/") + 1, model.Urn.Length - model.Urn.IndexOf("/") - 1);
                    var agency1 = model.Urn.Substring(0, model.Urn.IndexOf("/"));
                    var identifiertriple1 = new IdentifierTriple(new Guid(identifier1), 1, agency1);
                    GetSet(identifiertriple1);
                    if (model.DataSet == null) model.DataSet = new Collection<DataSetItem>();
                    if (model.ItemTypes == null) model.ItemTypes = new List<Item>();
                    model.Urns = new List<string>();
                    break;
                case "Display Tree":
                    var identifier2 = model.Urn.Substring(model.Urn.IndexOf("/") + 1, model.Urn.Length - model.Urn.IndexOf("/") - 1);
                    var agency2 = model.Urn.Substring(0, model.Urn.IndexOf("/"));
                    var identifiertriple2 = new IdentifierTriple(new Guid(identifier2), 1, agency2);
                    List<TreeViewNode> nodes2 = new List<TreeViewNode>();
                    nodes = BuildTree(identifiertriple2, nodes2);
                    ViewBag.Json = (new JavaScriptSerializer()).Serialize(nodes);
                    return View("Tree");
            }
            return View(model);
        }

        public ActionResult Tree()
        {
            DataSetModel model = new DataSetModel();
            model.DataSet = new Collection<DataSetItem>();
            model.ItemTypes = new List<Item>();
            model.Urns = new List<string>();
            return View(model);
        }

        [HttpPost]
        public ActionResult Tree(DataSetModel model, string command, HttpPostedFileBase postedFile)
        {
            
            switch (command)
            {
                case "Search":
                    var identifier = model.Urn.Substring(model.Urn.IndexOf("/") + 1, model.Urn.Length - model.Urn.IndexOf("/") - 1);
                    var agency = model.Urn.Substring(0, model.Urn.IndexOf("/"));
                    model = ProcessDataSet(model, agency, new Guid(identifier));
                    model.Urns = new List<string>();
                    var identifiertriple = new IdentifierTriple(new Guid(identifier), 1, agency);
                    List<TreeViewNode> nodes = new List<TreeViewNode>();
                    nodes = BuildTree(identifiertriple, nodes);
                    ViewBag.Json = (new JavaScriptSerializer()).Serialize(nodes);
                    return View(model);               
              
            }
            return View(model);
        }

        public DataSetModel ProcessAllData(DataSetModel model)
        {
            foreach (var urn in model.Urns)
            {
                var identifier = urn.Substring(urn.IndexOf("/") + 1, urn.Length - urn.IndexOf("/") - 1);
                var agency = urn.Substring(0, urn.IndexOf("/"));
                // ProcessDataSet(model, agency, new Guid(identifier));
                Collection<IdentifierTriple> identifiers =  GetCurrentItems(model, agency, new Guid(identifier));
                DeprecateAllItems(identifiers);
            }
            return model;
        }

        public DataSetModel ProcessDataSet(DataSetModel model, string agency, Guid id)
        {
            model = GetItems(model,agency, id);
            var types = from r in model.DataSet
                        group r by r.ItemType into r1
                        select new { Name = r1.Key.Replace(";",""), Value = r1.Count().ToString() };
            List<Item> itemtypes = new List<Item>();
            foreach (var type in types)
            {
                Item itemtype = new Item();
                itemtype.Id = type.Name;
                itemtype.QuestionValue = type.Value;
                itemtypes.Add(itemtype);
            }
            Item item = new Item();
            item.Id = "Total";
            item.QuestionValue = model.DataSet.Count().ToString();
            itemtypes.Add(item);
            model.ItemTypes = itemtypes;
            return model;
        }
        public DataSetModel GetItems(DataSetModel model,string agency, Guid id)
        {
            MultilingualString.CurrentCulture = "en-US";

            var client = ClientHelper.GetClient();
            long version = client.GetLatestVersionNumber(id, agency);

            IdentifierTriple variable1 = new IdentifierTriple(id, version, agency);
            Collection<IdentifierTriple> items = client.GetLatestSet(variable1);
            Collection<DataSetItem> ditems = new Collection<DataSetItem>();
            foreach (var item in items)
            {
                long version1 = client.GetLatestVersionNumber(item.Identifier, item.AgencyId);
                DataSetItem ditem = new DataSetItem();
                ditem.Agency = item.AgencyId;
                ditem.Identifier = item.Identifier;
                ditem.Version = item.Version;
                Item newItem = new Item();
                newItem = GetDetail(agency, item.Identifier);
                ditem.ItemType = newItem.Id;
                List<RepositoryItemMetadata> test = GetConcept(item.AgencyId,item.Identifier).OrderByDescending(a => a.IsDeprecated).ToList();
                // if (test.Count > 1) { test = GetConcept(item.AgencyId, item.Identifier).Where(a => a.IsDeprecated == true).ToList(); }
                ditem.IsDeprecated = test.FirstOrDefault().IsDeprecated;
                ditem.Referencing = test.FirstOrDefault().ItemType.ToString();
                // ditem.Version = test.Count;
                ditem.Version = test.FirstOrDefault().Version;

                // if (newItem.QuestionValue != null) { model.DisplayLabel =GetRepository(new Guid(newItem.QuestionValue)).Where(a => a.Identifier == id).FirstOrDefault().DisplayLabel; }
                ditems.Add(ditem);
            }
            model.DataSet = ditems;
            return model;
        }

        public Collection<IdentifierTriple> GetCurrentItems(DataSetModel model, string agency, Guid id)
        {
            MultilingualString.CurrentCulture = "en-US";

            var client = ClientHelper.GetClient();
            long version = client.GetLatestVersionNumber(id, agency);

            IdentifierTriple variable1 = new IdentifierTriple(id, version, agency);
            Collection<IdentifierTriple> items = client.GetLatestSet(variable1);            
            return items;
        }

        public Item GetDetail(string agency, Guid id)
        {
            MultilingualString.CurrentCulture = "en-US";

            var client = ClientHelper.GetClient();
            long version = client.GetLatestVersionNumber(id, agency);

            Item newitem = new Item();
            IdentifierTriple variable1 = new IdentifierTriple(id, version, agency);
            IVersionable item = client.GetLatestItem(id, agency);
            string itemtype = null;
            if (item is PhysicalInstance) { newitem.QuestionValue = item.ItemType.ToString(); }
            itemtype = DataItem(item);
            newitem.Id = itemtype;
            return newitem;
        }


        public List<RepositoryItemMetadata> GetConcept(string agency, Guid id)
        {
            MultilingualString.CurrentCulture = "en-US";

            var client = ClientHelper.GetClient();

            // Retrieve the requested item from the Repository.
            // Populate the item's children, so we can display information about them.
            IVersionable item = client.GetLatestItem(id, agency);
            // Use a graph search to find a list of all items that 
            // directly reference this item.
            GraphSearchFacet facet = new GraphSearchFacet();
            facet.TargetItem = item.CompositeId;
            facet.UseDistinctResultItem = true;
            var referencingItemsDescriptions = client.GetRepositoryItemDescriptionsByObject(facet);
            List<RepositoryItemMetadata> referenceItems = referencingItemsDescriptions.ToList();
            var items = client.GetRepositorySettings();
            return referenceItems;
        }

        public List<SearchResult> GetRepository(Guid itemType)
        {
            DateTime start, finish;
            MultilingualString.CurrentCulture = "en-US";

            // Create a new SearchFacet that will find all
            // StudyUnits, CodeSchemes, and CategorySchemes.

            SearchFacet facet = new SearchFacet();
            facet.ItemTypes.Add(itemType);
             

            facet.ResultOrdering = SearchResultOrdering.ItemType;
            facet.SearchLatestVersion = true;

            // Add SearchTerms to the facet to only return results that contain the specified text.

            // Add Cultures to only search for text in certain languages.
            //facet.Cultures.Add("da-DK");

            // Use MaxResults and ResultOffset to implement paging, if large numbers of items may be returned.
            //facet.MaxResults = pageSize;
            //facet.ResultOffset = (pageSize * page);

            // Now that we have a facet, search for the items in the Repository.
            // The client object takes care of making the Web Services calls.
            start = DateTime.Now;
            var client = ClientHelper.GetClient();
            SearchResponse response = client.Search(facet);          
            // Create the model object, and add all the search results to 
            // the model's list of results so they can be displayed.)
            IEnumerable<SearchResult> results = response.Results;
            List<SearchResult> results1 = new List<SearchResult>();
            results = results.ToList();

            return results.ToList();
        }

        public string DataItem(IVersionable item)
        {
            string itemtype = "Not Defined";
            if (item is PhysicalInstance) { itemtype = "Physical Instance"; }
            if (item is Archive) { itemtype = "Archive"; }
            if (item is Category) { itemtype = "Category"; };
            if (item is CategoryGroup) { itemtype = "Category Group"; }
            if (item is CategoryScheme) { itemtype = "Category Scheme"; }
            if (item is CodeList) { itemtype = "Code List"; }
            if (item is CodeListGroup) { itemtype = "Code List Group"; }
            if (item is CodeListScheme) { itemtype = "Code List Scheme"; }
            if (item is Concept) { itemtype = "Concept"; }
            if (item is ConceptGroup) { itemtype = "Concept Group"; }
            if (item is ConceptScheme) { itemtype = "Concept Scheme"; }
            if (item is ConceptualComponent) { itemtype = "Concept Componant"; }
            if (item is ConceptualVariable) { itemtype = "Concepteptual Variable"; }
            if (item is ConceptualVariableGroup) { itemtype = "Concepteptual Variable Group"; }
            if (item is ConceptualVariableScheme) { itemtype = "Concepteptual Variable Scheme"; }
            if (item is Condition) { itemtype = "Condition"; }
            if (item is DataCollection) { itemtype = "Data Collection"; }
            if (item.ItemType == new Guid("f39ff278-8500-45fe-a850-3906da2d242b")) { itemtype = "Data Layout"; }
            if (item is DataItem) { itemtype = "Data Item"; }
            if (item is Group) { itemtype = "Group"; }
            if (item is Instrument) { itemtype = "Instrument"; }
            if (item is InstrumentScheme) { itemtype = "Instrument Scheme"; }
            if (item is InstrumentGroup) { itemtype = "Instrument Group"; }
            if (item is InterviewerInstruction) { itemtype = "Organization Group"; }
            if (item is InterviewerInstructionScheme) { itemtype = "Organization"; }
            if (item is LogicalProduct) { itemtype = "Organization Group"; }
            if (item is ManagedRepresentationGroup) { itemtype = "Organization Relationship"; }
            if (item is ManagedRepresentationScheme) { itemtype = "Organization Scheme"; }
            if (item is Organization) { itemtype = "Organization"; }
            if (item is OrganizationGroup) { itemtype = "Organization Group"; }
            if (item is OrganizationRelationship) { itemtype = "Organization Relationship"; }
            if (item is OrganizationScheme) { itemtype = "Organization Scheme"; }
            if (item is OtherMaterial) { itemtype = "Other Material"; }
            if (item is OtherMaterialsGroup) { itemtype = "Other Material Group"; }
            if (item is PhysicalProduct) { itemtype = "Physical Product"; }
            if (item is PhysicalStructure) { itemtype = "Physical Structure"; }
            if (item is ProcessingEvent) { itemtype = "Processing Event"; }
            if (item is ProcessingEventGroup) { itemtype = "Processing Event Group"; }
            if (item is ProcessingInstructionGroup) { itemtype = "Processing Instruction Group"; }
            if (item is ProcessingInstructionScheme) { itemtype = "Processing Instruction Scheme"; }
            if (item is QualityStandard) { itemtype = "Quality Standard"; }
            if (item is QualityStatement) { itemtype = "Qualtiy Statement"; }
            if (item is QualityStatementGroup) { itemtype = "Quality Statement Group"; }
            if (item is QualityStatementItem) { itemtype = "Quality Statement Item"; }
            if (item is RepresentedVariable) { itemtype = "Represented Variable"; }
            if (item is RepresentedVariableGroup) { itemtype = "Represented Variable Group"; }
            if (item is RepresentedVariableScheme) { itemtype = "Represented Variable Scheme"; }
            if (item is SeriesStatement) { itemtype = "Series Statement"; }
            if (item is StudyUnit) { itemtype = "Study"; }
            if (item is Variable) { itemtype = "Variable"; }
            if (item is VariableGroup) { itemtype = "Variable Group"; }
            if (item is VariableScheme) { itemtype = "Variable Scheme"; }
            if (item is VariableStatistic) { itemtype = "Variable Statistic"; }
            return itemtype;
        }

        public void DeprecateItems(DataSetModel model)
        {
            Collection<IdentifierTriple> identifiers = new Collection<IdentifierTriple>();
            foreach (var item in model.DataSet)
            {
                IdentifierTriple identifier = new IdentifierTriple(item.Identifier, item.Version, item.Agency);
                identifiers.Add(identifier);
            }
            var client = ClientHelper.GetClient();
            client.UpdateDeprecatedState(identifiers, true, false);
            CreatePowerShellScript(model);

            // this procedure is for when it is possible to remove items using C#
            //foreach (var item in model.DataSet)
            //{
            //    RemoveItemAsync(item.Identifier, item.Agency, item.Version);
            //}
        }

        public void DeprecateAllItems(Collection<IdentifierTriple> identifiers)
        {           
            var client = ClientHelper.GetClient();
            client.UpdateDeprecatedState(identifiers, true, false);
        }

        public void CreatePowerShellScript(DataSetModel model)
        {
            
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"C:\Users\qtnvwhn\ElasticDelete.ps1"))
            {
                foreach (var item in model.DataSet)
                {
                    if (item.ItemType == "Variable")
                    {
                        string line;
                        line = "Invoke-WebRequest -Method DELETE -Uri " + "http://127.0.0.1:9200/clsr-clrdp2w01p.ad.ucl.ac.uk_registered_item/_doc/" + item.Agency + ":" + item.Identifier.ToString() + ":" + item.Version;
                        file.WriteLine(line);
                    }
                }
            }
        }

        public async void RemoveItemAsync(Guid id, string agency, long version)
        {
            var indexname = ConfigurationManager.AppSettings["URL"].ToString();
            var command = indexname + "/_doc/" + agency + ":" + id.ToString() + ":" + version.ToString();

            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("DELETE"), command))
                {
                    var response = await httpClient.SendAsync(request);
                }
            }
        }

        public Collection<IdentifierTriple> GetSet(IdentifierTriple identifier)
        {
            Collection<IdentifierTriple> identifiers = new Collection<IdentifierTriple>();
          
            var client = ClientHelper.GetClient();
            identifiers = client.GetSet(identifier);
            return identifiers;
        }

        private List<TreeViewNode> BuildTree(IdentifierTriple identifier, List<TreeViewNode> nodes)
        {
            List<RepositoryItemMetadata> children = new List<RepositoryItemMetadata>();
            Collection<IdentifierTriple> identifiers = GetSet(identifier);
            var client = ClientHelper.GetClient();
            int i = 1;
            identifiers = GetSet(identifier);
            foreach (var result in identifiers)
            {
                IdentifierTriple it = new IdentifierTriple(result.Identifier, 1, result.AgencyId);
                IVersionable item = client.GetLatestItem(result.Identifier, result.AgencyId);
                var item2 = client.GetLatestRepositoryItem(result.Identifier, result.AgencyId);
                var test = client.GetRepositoryItemDescription(result.Identifier, result.AgencyId, 1);
                var newItem = GetDetail(result.AgencyId, result.Identifier);
                string itemname, label;
                if (test.ItemName.Count != 0) { itemname = test.ItemName.FirstOrDefault().Value.ToString(); } else { itemname = "No Question Name"; }
                if (test.Label.Count != 0) { label = test.Label.FirstOrDefault().Value.ToString(); } else { label = "No Question Text"; }

                if (newItem.Id == "Variable")
                {
                    nodes.Add(new TreeViewNode { id = result.Identifier.ToString(), parent = "#", text = newItem.Id + " - " + result.AgencyId + " - " + result.Identifier.ToString() + " - " + "Name: " + itemname + " - " + "Question: " + label + " -  Deprecated = " + item2.IsDeprecated });
                    // nodes = BuildChildrenTree(result, nodes);
                    i++;
                }

            }
            nodes = nodes.OrderBy(a => a.text).ToList();
            return nodes;
        }

        private List<TreeViewNode> BuildChildrenTree(IdentifierTriple identifier, List<TreeViewNode> nodes)
        {
            List<RepositoryItemMetadata> children = new List<RepositoryItemMetadata>();
            children = GetConcept(identifier.AgencyId, identifier.Identifier);
            int i = 1;
            foreach (var child in children)
            {
                var newItem = GetDetail(child.AgencyId, child.Identifier);
                nodes.Add(new TreeViewNode { id = identifier.Identifier.ToString() + " " + child.Identifier.ToString(), parent = identifier.Identifier.ToString(), text = newItem.Id + " - " +child.Identifier.ToString() + " - " + child.IsDeprecated });
                i++;
            }
            return nodes;
        }

    }
}