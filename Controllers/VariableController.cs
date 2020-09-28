using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DeprecateItems.Controllers
{
    public class VariableController : Controller
    {
        // GET: Variable
        public ActionResult Index()
        {
            return View();
        }

        private async void RenameVariables()
        {
            RepositoryClientBase client =  null; // TODO Fill this in.
            IdentifierTriple[] idList = new[] { new IdentifierTriple(new Guid(), 1, "") }; // TODO Get appropriate IDs.

            // Set up a transaction.
            var transaction = await client.CreateTransactionAsync();
            var request = new RepositoryTransactionAddItemsRequest();
            request.TransactionId = transaction.TransactionId;

            foreach (var id in idList)
            {
                var item = client.GetItem(id) as Variable;
                item.ItemName["en-GB"] = "MyNewName";
                RepositoryItem repoItem = GetRepositoryItemFromVersionable(client, item);
                request.Items.Add(repoItem);
            }


            // Push the transaction.
            var addItemsResult = await client.AddItemsToTransactionAsync(request);

            var commitOptions = new RepositoryTransactionCommitOptions();
            commitOptions.TransactionType = RepositoryTransactionType.CommitAsLatestWithLatestChildrenAndPropagateVersions;
            var commitResult = await client.CommitTransactionAsync(commitOptions);

        }

        private static RepositoryItem GetRepositoryItemFromVersionable(RepositoryClientBase client, Variable item)
        {
            RepositoryItem repositoryItem = new RepositoryItem()
            {
                CompositeId = item.CompositeId,
                Item = client.GetRepresentation(item, RepositoryFormats.Ddi33),
                ItemType = item.ItemType,
                ItemFormat = RepositoryFormats.Ddi33,
                //IsDepricated
                IsPublished = item.IsPublished,
                Notes = new Collection<Note>(),
                VersionDate = item.VersionDate,
                VersionRationale = item.VersionRationale,
                VersionResponsibility = item.VersionResponsibility,
                Tag = item
            };
            return repositoryItem;
        }
    }
}