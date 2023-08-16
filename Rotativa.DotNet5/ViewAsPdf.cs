
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rotativa.DotNet5;
using Rotativa.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rotativa;

internal class ViewAsPdf : AsPdfResultBase
{
	private string _masterName;

	public string MasterName
	{
		get { return _masterName ?? string.Empty; }
		set { _masterName = value; }
	}

	public new object Model { get; set; } = null;

	protected ViewAsPdf(RotativaPath rotativaPath, ViewDataDictionary viewData = null) : base(rotativaPath)
	{
		WkhtmlPath = string.Empty;
		MasterName = string.Empty;
		ViewName = string.Empty;
		ViewData = viewData;
	}

	public ViewAsPdf(RotativaPath rotativaPath, string viewName, ViewDataDictionary viewData = null)
		: this(rotativaPath, viewData)
	{
		ViewName = viewName;
	}

	public ViewAsPdf(RotativaPath rotativaPath, object model, ViewDataDictionary viewData = null)
		: this(rotativaPath, viewData)
	{
		Model = model;
	}

	public ViewAsPdf(RotativaPath rotativaPath, string viewName, object model, ViewDataDictionary viewData = null)
		: this(rotativaPath, model, viewData)
	{
		ViewName = viewName;
	}

	public ViewAsPdf(RotativaPath rotativaPath, string viewName, string masterName, object model) : this(rotativaPath, viewName, model)
	{
		MasterName = masterName;
	}

	protected override string GetUrl(ActionContext context)
	{
		return string.Empty;
	}

	protected virtual ViewEngineResult GetView(ActionContext context, string viewName, string masterName)
	{
		var engine = context.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;

		var getViewResult = engine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: true);
		if (getViewResult.Success)
			return getViewResult;

		var findViewResult = engine.FindView(context, viewName, isMainPage: true);
		if (findViewResult.Success)
			return findViewResult;

		var searchedLocations = getViewResult.SearchedLocations.Concat(findViewResult.SearchedLocations);
		var errorMessage = string.Join(
			Environment.NewLine,
			new[ ] { $"Unable to find view '{viewName}'. The following locations were searched:" }.Concat(searchedLocations));

		throw new InvalidOperationException(errorMessage);
	}

	protected override async Task<byte[ ]> CallTheDriver(ActionContext context)
	{
		// use action name if the view name was not provided
		var viewName = ViewName;
		if (string.IsNullOrEmpty(ViewName))
			viewName = ( (Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)context.ActionDescriptor ).ActionName;

		ViewEngineResult viewResult = GetView(context, viewName, MasterName);
		var html = new StringBuilder( );

		//string html = context.GetHtmlFromView(viewResult, viewName, Model);
		var tempDataProvider = context.HttpContext.RequestServices.GetService(typeof(ITempDataProvider)) as ITempDataProvider;

		var viewDataDictionary = new ViewDataDictionary(
			metadataProvider: new EmptyModelMetadataProvider( ),
			modelState: new ModelStateDictionary( ))
		{
			Model = Model
		};
		if (ViewData != null)
		{
			foreach (var item in ViewData)
			{
				viewDataDictionary.Add(item);
			}
		}
		using (var output = new StringWriter( ))
		{
			var view = viewResult.View;
			var tempDataDictionary = new TempDataDictionary(context.HttpContext, tempDataProvider);
			var viewContext = new ViewContext(
				context,
				viewResult.View,
				viewDataDictionary,
				tempDataDictionary,
				output,
				new HtmlHelperOptions( ));

			await view.RenderAsync(viewContext);

			html = output.GetStringBuilder( );
		}


		var baseUrl = string.Format("{0}://{1}", context.HttpContext.Request.Scheme, context.HttpContext.Request.Host);
		var htmlForWkhtml = Regex.Replace(html.ToString( ), "<head>", string.Format("<head><base href=\"{0}\" />", baseUrl), RegexOptions.IgnoreCase);

		var fileContent = WkhtmltopdfDriver.ConvertHtml(WkhtmlPath, GetConvertOptions( ), htmlForWkhtml);
		return fileContent;
	}
}
