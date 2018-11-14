using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Speak.Server.Requests;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.Globalization;
using Sitecore.Pipelines.HasPresentation;
using Sitecore.Shell.Applications.WebEdit.Commands;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;

namespace Sitecore.Support.XA.Foundation.VersionSpecific.Requests
{
    public class InsertRequest : PipelineProcessorRequest<ItemNameAndTemplateContext>
    {
        public override PipelineProcessorResponseValue ProcessRequest()
        {
            RequestContext.ValidateContextItem();

            Assert.IsNotNullOrEmpty(RequestContext.Name, "Could not get item name for request args:{0}", Args.Data);
            Assert.IsNotNullOrEmpty(RequestContext.TemplateItemId, "The template id:{0} is null or empty, request args:{1}", RequestContext.TemplateItemId, Args.Data);

            New newCommand = new New();

            BranchItem branch = RequestContext.ParentItem.Database.Branches[ShortID.Parse(RequestContext.TemplateItemId).ToID().ToString(), RequestContext.Item.Language];

            Assert.IsNotNull(branch, typeof(BranchItem));

            newCommand.ExecuteCommand(RequestContext.ParentItem, branch, RequestContext.Name);

            Client.Site.Notifications.Disabled = true;
            Item createdItem = Context.Workflow.AddItem(RequestContext.Name, branch, RequestContext.ParentItem);
            Client.Site.Notifications.Disabled = false;

            if (createdItem == null)
            {
                return new PipelineProcessorResponseValue
                {
                    AbortMessage = Translate.Text("Could not create item.")
                };
            }
            newCommand.PolicyBasedUnlock(createdItem);
            using (new DeviceSwitcher(RequestContext.DeviceItem))
            {
                return new PipelineProcessorResponseValue
                {
                    Value = new
                    {
                        //the only change to the original InsertRequest
                        itemId = GetItemId(createdItem)
                    }
                };
            }
        }

        protected virtual string GetItemId(Item createdItem)
        {
            if (createdItem.Template.DoesTemplateInheritFrom(Sitecore.XA.Foundation.Presentation.Templates.PageDesignFolder.ID) ||
                createdItem.Template.DoesTemplateInheritFrom(Sitecore.XA.Foundation.Presentation.Templates.PartialDesignFolder.ID))
            {
                return RequestContext.ItemId;
            }
            return GetFirstParentWithPresentation(createdItem);
        }

        protected virtual string GetFirstParentWithPresentation(Item item)
        {
            if (item == null)
            {
                return null;
            }
            if (!HasPresentationPipeline.Run(item))
            {
                return GetFirstParentWithPresentation(item.Parent);
            }
            return item.ID.ToString();
        }
    }
}