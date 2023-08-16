using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rotativa.Infrastructure;
using Rotativa.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rotativa;

internal abstract class AsResultBase : ViewResult
{
	protected AsResultBase(RotativaPath rotativaPath)
	{
		WkhtmlPath = rotativaPath.Path;
		FormsAuthenticationCookieName = ".ASPXAUTH";
	}

	/// <summary>
	/// This will be send to the browser as a name of the generated PDF file.
	/// </summary>
	public string FileName { get; set; }

	/// <summary>
	/// Path to wkhtmltopdf\wkhtmltoimage binary.
	/// </summary>
	public string WkhtmlPath { get; set; }

	/// <summary>
	/// Custom name of authentication cookie used by forms authentication.
	/// </summary>
	[Obsolete("Use FormsAuthenticationCookieName instead of CookieName.")]
	public string CookieName
	{
		get { return FormsAuthenticationCookieName; }
		set { FormsAuthenticationCookieName = value; }
	}

	/// <summary>
	/// Custom name of authentication cookie used by forms authentication.
	/// </summary>
	public string FormsAuthenticationCookieName { get; set; }

	/// <summary>
	/// Sets custom headers.
	/// </summary>
	[OptionFlag("--custom-header")]
	public Dictionary<string, string> CustomHeaders { get; set; }

	/// <summary>
	/// Sets cookies.
	/// </summary>
	[OptionFlag("--cookie")]
	public Dictionary<string, string> Cookies { get; set; }

	/// <summary>
	/// Sets post values.
	/// </summary>
	[OptionFlag("--post")]
	public Dictionary<string, string> Post { get; set; }

	/// <summary>
	/// Indicates whether the page can run JavaScript.
	/// </summary>
	[OptionFlag("-n")]
	public bool IsJavaScriptDisabled { get; set; }

	/// <summary>
	/// Minimum font size.
	/// </summary>
	[OptionFlag("--minimum-font-size")]
	public int? MinimumFontSize { get; set; }

	/// <summary>
	/// Sets proxy server.
	/// </summary>
	[OptionFlag("-p")]
	public string Proxy { get; set; }

	/// <summary>
	/// HTTP Authentication username.
	/// </summary>
	[OptionFlag("--username")]
	public string UserName { get; set; }

	/// <summary>
	/// HTTP Authentication password.
	/// </summary>
	[OptionFlag("--password")]
	public string Password { get; set; }

	/// <summary>
	/// Use this if you need another switches that are not currently supported by Rotativa.
	/// </summary>
	[OptionFlag("")]
	public string CustomSwitches { get; set; }

	public ContentDisposition ContentDisposition { get; set; }

	protected abstract string GetUrl(ActionContext context);

	/// <summary>
	/// Returns properties with OptionFlag attribute as one line that can be passed to wkhtmltopdf binary.
	/// </summary>
	/// <returns>Command line parameter that can be directly passed to wkhtmltopdf binary.</returns>
	protected virtual string GetConvertOptions( )
	{
		var result = new StringBuilder( );

		var fields = GetType( ).GetProperties( );
		foreach (var fi in fields)
		{
			var of = fi.GetCustomAttributes(typeof(OptionFlag), true).FirstOrDefault( ) as OptionFlag;
			if (of == null)
				continue;

			var value = fi.GetValue(this, null);
			if (value == null)
				continue;

			if (fi.PropertyType == typeof(Dictionary<string, string>))
			{
				var dictionary = (Dictionary<string, string>)value;
				foreach (var d in dictionary)
				{
					result.AppendFormat(" {0} {1} {2}", of.Name, d.Key, d.Value);
				}
			}
			else if (fi.PropertyType == typeof(bool))
			{
				if ((bool)value)
					result.AppendFormat(CultureInfo.InvariantCulture, " {0}", of.Name);
			}
			else
			{
				result.AppendFormat(CultureInfo.InvariantCulture, " {0} {1}", of.Name, value);
			}
		}

		return result.ToString( ).Trim( );
	}

	private string GetWkParams(ActionContext context)
	{
		var switches = string.Empty;

		string authenticationCookie = null;
		if (context.HttpContext.Request.Cookies != null && context.HttpContext.Request.Cookies.Keys.Contains(FormsAuthenticationCookieName))
			authenticationCookie = context.HttpContext.Request.Cookies[FormsAuthenticationCookieName];
		if (authenticationCookie != null)
			switches += " --cookie " + FormsAuthenticationCookieName + " " + authenticationCookie;

		switches += " " + GetConvertOptions( );

		var url = GetUrl(context);
		switches += " " + url;

		return switches;
	}

	protected virtual Task<byte[ ]> CallTheDriver(ActionContext context)
	{
		var switches = GetWkParams(context);
		var fileContent = WkhtmlConvert(switches);
		return Task.FromResult(fileContent);
	}

	protected abstract byte[ ] WkhtmlConvert(string switches);

	public async Task<byte[ ]> BuildFile(ActionContext context)
	{
		if (context == null)
			throw new ArgumentNullException("context");

		return await CallTheDriver(context);
	}

	public virtual async Task<bool> SaveFileOnServerAsync(ActionContext context, string saveOnServerPath)
	{
		if (saveOnServerPath is null || !Uri.IsWellFormedUriString(saveOnServerPath, UriKind.RelativeOrAbsolute))
			return false;

		File.WriteAllBytes(saveOnServerPath, await BuildFile(context));
		return true;
	}

	public async override Task ExecuteResultAsync(ActionContext context)
	{
		var fileContent = await BuildFile(context);

		var response = PrepareResponse(context.HttpContext.Response);

		await response.Body.WriteAsync(fileContent);
	}

	private static string SanitizeFileName(string name)
	{
		var invalidChars = Regex.Escape(new string(Path.GetInvalidPathChars( )) + new string(Path.GetInvalidFileNameChars( )));
		var invalidCharsPattern = string.Format(@"[{0}]+", invalidChars);

		var result = Regex.Replace(name, invalidCharsPattern, "_");
		return result;
	}

	protected HttpResponse PrepareResponse(HttpResponse response)
	{
		response.ContentType = GetContentType( );

		if (!string.IsNullOrEmpty(FileName))
		{
			var contentDisposition = ContentDisposition == ContentDisposition.Attachment
				? "attachment"
				: "inline";

			response.Headers.Add("Content-Disposition", string.Format("{0}; filename=\"{1}\"", contentDisposition, SanitizeFileName(FileName)));
		}
		//response.Headers.Add("Content-Type", this.GetContentType());

		return response;
	}

	protected abstract string GetContentType( );
}
