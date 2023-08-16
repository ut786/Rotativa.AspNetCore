using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Rotativa.Infrastructure;


public interface IRotativaConverter
{
	IActionResult ToPdfResult(string viewName, ViewDataDictionary viewData = null);
	IActionResult ToPdfResult(string viewName, object model, ViewDataDictionary viewData = null);
	IActionResult ToPdfResult(object model, ViewDataDictionary viewData = null);
	IActionResult ToPdfResult(string viewName, string masterName, object model);
}

internal class RotativaConverter : IRotativaConverter
{
	private readonly RotativaPath _rotativaPath;

	public RotativaConverter(RotativaPath rotativaPath)
	{
		_rotativaPath = rotativaPath;
	}


	public IActionResult ToPdfResult(string viewName, ViewDataDictionary viewData = null) => new ViewAsPdf(_rotativaPath, viewName, viewData);
	public IActionResult ToPdfResult(object model, ViewDataDictionary viewData = null) => new ViewAsPdf(_rotativaPath, model, viewData);
	public IActionResult ToPdfResult(string viewName, object model, ViewDataDictionary viewData = null) => new ViewAsPdf(_rotativaPath, viewName, model, viewData);
	public IActionResult ToPdfResult(string viewName, string masterName, object model) => new ViewAsPdf(_rotativaPath, viewName, masterName, model);
}
