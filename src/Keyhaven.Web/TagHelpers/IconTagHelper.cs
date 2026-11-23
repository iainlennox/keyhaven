using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Keyhaven.Web.TagHelpers;

[HtmlTargetElement("icon")]
public class IconTagHelper : TagHelper
{
    [HtmlAttributeName("name")]
    public string Name { get; set; } = "dot";

    private static readonly Dictionary<string, string> Paths = new()
    {
        ["dashboard"] = "<rect x='3' y='3' width='7' height='9' rx='1.5'/><rect x='14' y='3' width='7' height='5' rx='1.5'/><rect x='14' y='12' width='7' height='9' rx='1.5'/><rect x='3' y='16' width='7' height='5' rx='1.5'/>",
        ["building"] = "<path d='M3 21h18'/><path d='M5 21V5a2 2 0 0 1 2-2h6a2 2 0 0 1 2 2v16'/><path d='M15 9h2a2 2 0 0 1 2 2v10'/><path d='M9 7h2'/><path d='M9 11h2'/><path d='M9 15h2'/>",
        ["key"] = "<circle cx='7.5' cy='15.5' r='3.5'/><path d='m10 13 9-9'/><path d='m16 4 3 3'/><path d='m13 7 2 2'/>",
        ["wrench"] = "<path d='M14.7 6.3a4 4 0 0 0-5.4 5.4L3 18l3 3 6.3-6.3a4 4 0 0 0 5.4-5.4l-2.6 2.6-2.4-2.4z'/>",
        ["shield"] = "<path d='M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z'/><path d='m9 12 2 2 4-4'/>",
        ["calendar"] = "<rect x='3' y='5' width='18' height='16' rx='2'/><path d='M16 3v4'/><path d='M8 3v4'/><path d='M3 10h18'/>",
        ["mail"] = "<rect x='3' y='5' width='18' height='14' rx='2'/><path d='m3 7 9 6 9-6'/>",
        ["wallet"] = "<path d='M20 12V8a2 2 0 0 0-2-2H5a2 2 0 0 1 0-4h13'/><path d='M3 6v12a2 2 0 0 0 2 2h15a1 1 0 0 0 1-1v-4'/><path d='M21 12h-5a2 2 0 0 0 0 4h5z'/>",
        ["users"] = "<path d='M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2'/><circle cx='9' cy='7' r='4'/><path d='M22 21v-2a4 4 0 0 0-3-3.87'/><path d='M16 3.13a4 4 0 0 1 0 7.75'/>",
        ["doc"] = "<path d='M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z'/><path d='M14 2v6h6'/><path d='M8 13h8'/><path d='M8 17h5'/>",
        ["bell"] = "<path d='M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9'/><path d='M13.7 21a2 2 0 0 1-3.4 0'/>",
        ["search"] = "<circle cx='11' cy='11' r='7'/><path d='m21 21-4.3-4.3'/>",
        ["plus"] = "<path d='M12 5v14'/><path d='M5 12h14'/>",
        ["check"] = "<path d='m4 12 5 5L20 6'/>",
        ["alert"] = "<path d='M12 3 2 21h20L12 3z'/><path d='M12 9v5'/><path d='M12 17h.01'/>",
        ["arrow-right"] = "<path d='M5 12h14'/><path d='m13 6 6 6-6 6'/>",
        ["chevron-right"] = "<path d='m9 18 6-6-6-6'/>",
        ["chevron-down"] = "<path d='m6 9 6 6 6-6'/>",
        ["logout"] = "<path d='M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4'/><path d='m16 17 5-5-5-5'/><path d='M21 12H9'/>",
        ["chart"] = "<path d='M3 3v18h18'/><rect x='7' y='12' width='3' height='6'/><rect x='12' y='8' width='3' height='10'/><rect x='17' y='5' width='3' height='13'/>",
        ["sparkles"] = "<path d='m12 3 1.9 4.6L18.5 9.5l-4.6 1.9L12 16l-1.9-4.6L5.5 9.5l4.6-1.9z'/><path d='M19 15l.9 2.1L22 18l-2.1.9L19 21l-.9-2.1L16 18l2.1-.9z'/><path d='M5 3l.7 1.6L7.3 5l-1.6.7L5 7.3l-.7-1.6L2.7 5l1.6-.4z'/>",
        ["home"] = "<path d='m3 10 9-7 9 7v10a1 1 0 0 1-1 1h-5v-7H9v7H4a1 1 0 0 1-1-1z'/>",
        ["clock"] = "<circle cx='12' cy='12' r='9'/><path d='M12 7v5l3 2'/>",
        ["phone"] = "<path d='M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.1 4.2 2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1.9.3 1.8.6 2.6a2 2 0 0 1-.4 2.1L8 9.8a16 16 0 0 0 6 6l1.4-1.4a2 2 0 0 1 2.1-.4c.8.3 1.7.5 2.6.6a2 2 0 0 1 1.9 2.3z'/>",
        ["filter"] = "<path d='M3 5h18l-7 8v6l-4 2v-8z'/>",
        ["send"] = "<path d='m22 2-7 20-4-9-9-4z'/><path d='M22 2 11 13'/>",
        ["x"] = "<path d='M18 6 6 18'/><path d='m6 6 12 12'/>",
        ["user"] = "<circle cx='12' cy='8' r='4'/><path d='M4 21a8 8 0 0 1 16 0'/>",
        ["pound"] = "<path d='M18 7a5 5 0 0 0-9-3 5 5 0 0 0-2 4c0 5-2 6-2 6h10'/><path d='M6 14h8'/><path d='M7 21h10'/>",
        ["map-pin"] = "<path d='M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0z'/><circle cx='12' cy='10' r='3'/>",
        ["clipboard"] = "<rect x='4' y='4' width='16' height='17' rx='2'/><path d='M9 4V3h6v1'/><path d='M9 9h6'/><path d='M9 13h6'/><path d='M9 17h4'/>",
        ["lock"] = "<rect x='4' y='11' width='16' height='10' rx='2'/><path d='M8 11V7a4 4 0 0 1 8 0v4'/>",
        ["refresh"] = "<path d='M21 12a9 9 0 1 1-3-6.7'/><path d='M21 3v6h-6'/>",
        ["settings"] = "<circle cx='12' cy='12' r='3'/><path d='M19.4 15a1.7 1.7 0 0 0 .3 1.9l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.9-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1-1.5 1.7 1.7 0 0 0-1.9.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.9 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1 1.7 1.7 0 0 0-.3-1.9l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.9.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.9-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.9V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z'/>",
        ["inbox"] = "<path d='M22 12h-6l-2 3h-4l-2-3H2'/><path d='M5.5 5h13l3.5 7v7a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-7z'/>",
        ["eye"] = "<path d='M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z'/><circle cx='12' cy='12' r='3'/>",
        ["download"] = "<path d='M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4'/><path d='m7 10 5 5 5-5'/><path d='M12 15V3'/>",
        ["dot"] = "<circle cx='12' cy='12' r='4'/>",
        ["menu"] = "<path d='M4 6h16'/><path d='M4 12h16'/><path d='M4 18h16'/>",
        ["star"] = "<path d='m12 2.5 2.9 6 6.6.9-4.8 4.6 1.2 6.5-5.9-3.2-5.9 3.2 1.2-6.5L2.5 9.4l6.6-.9z'/>"
    };

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "icon");
        output.Attributes.SetAttribute("viewBox", "0 0 24 24");
        output.Attributes.SetAttribute("fill", "none");
        output.Attributes.SetAttribute("stroke", "currentColor");
        output.Attributes.SetAttribute("stroke-width", "1.7");
        output.Attributes.SetAttribute("stroke-linecap", "round");
        output.Attributes.SetAttribute("stroke-linejoin", "round");
        output.Attributes.SetAttribute("aria-hidden", "true");
        output.Content.SetHtmlContent(Paths.TryGetValue(Name.ToLowerInvariant(), out var path) ? path : Paths["dot"]);
    }
}
