using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;

namespace SharePointFolderToLocal
{
	class Program
	{
		static void Main(string[] args)
		{
			ClientContext _ctx = new ClientContext("<SharePoint URL>");

			//load the list from SharePoint
			List spList = _ctx.Web.Lists.GetByTitle("<SharePoint List Name>");
			_ctx.Load(spList);
			_ctx.ExecuteQuery();

			DownloadFilesAndFolders(_ctx, spList, "<SP relative URL, null if complete list>", "<Local folder location absolute>");
		}

		static void DownloadFilesAndFolders(ClientContext _ctx, List spList, string spRelativeURL, string localFolder)
		{
			try
			{
				
				if (spRelativeURL == null)
				{
					//if sp url is null, get the root folder of list/library
					spRelativeURL = spList.RootFolder.ServerRelativeUrl;
				}

				if (spList != null && spList.ItemCount > 0)
				{
					//create caml query to load the items efficiently
					CamlQuery camlQuery = new CamlQuery();
					camlQuery.FolderServerRelativeUrl = spRelativeURL;

					//specifying items to load in a single query
					ListItemCollection listItems = spList.GetItems(camlQuery);
					_ctx.Load(listItems, item => item.Include(x => x.File, x => x.ContentType, x => x["Created"], x => x["CreatedById"], x => x["Modified"], x => x["Title"], x => x["Author"], x => x["Editor"], x => x["FileLeafRef"]));
					_ctx.ExecuteQuery();

					foreach (ListItem listItem in listItems)
					{
						if (listItem.ContentType.Name.ToLower() == "folder")
						{
							//creating folder on local if the sp list item is folder
							if (!System.IO.Directory.Exists(localFolder + listItem["FileLeafRef"]))
							{
								System.IO.Directory.CreateDirectory(localFolder + listItem["FileLeafRef"]);
							}

							//calling recursively to go through each folder
							DownloadFilesAndFolders(_ctx, spList, spRelativeURL + listItem["FileLeafRef"] + "/", localFolder + listItem["FileLeafRef"] + "\\");
						}
						else
						{
							//start downloding the file
							File fle = listItem.File;
							FileInformation fileInfo = File.OpenBinaryDirect(_ctx, fle.ServerRelativeUrl);

							string filePath = localFolder + fle.Name;

							System.IO.FileInfo fi = new System.IO.FileInfo(filePath);
							if (!fi.Exists)
							{
								using (System.IO.FileStream str = System.IO.File.Create(filePath))
								{
									fileInfo.Stream.CopyTo(str);
									str.Flush();
									str.Dispose();
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}
	}
}
