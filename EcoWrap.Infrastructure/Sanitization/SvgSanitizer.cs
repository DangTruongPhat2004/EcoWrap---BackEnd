// EcoWrap.Infrastructure/Sanitization/SvgSanitizer.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace EcoWrap.Infrastructure.Sanitization
{
    public interface ISvgSanitizer
    {
        Task<Stream> SanitizeAsync(Stream input, CancellationToken ct = default);
    }

    /// <summary>
    /// SvgSanitizer: loại bỏ các payload nguy hiểm trong SVG
    /// - Chặn XXE/DTD, vô hiệu XmlResolver
    /// - Xoá script, foreignObject, style (CSS nguy hiểm), iframe/object/embed
    /// - Xoá thuộc tính on* (onclick, onload, ...)
    /// - Siết chặt href/xlink:href (chỉ cho phép #fragment hoặc http/https)
    /// - Lọc style inline có url()/behavior/-moz-binding
    /// - Xuất lại XML UTF-8 (không BOM)
    /// </summary>
    public sealed class SvgSanitizer : ISvgSanitizer
    {
        // XPath các node cần loại bỏ
        // Lưu ý: có cả prefix svg:* để hỗ trợ tài liệu có namespace chuẩn.
        private const string DangerousNodesXPath =
            "//script | //svg:script | " +
            "//foreignObject | //svg:foreignObject | " +
            "//style | //svg:style | " +
            "//iframe | //object | //embed";

        public Task<Stream> SanitizeAsync(Stream input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            ct.ThrowIfCancellationRequested();

            try
            {
                input.Position = 0;

                // Cấu hình XML reader an toàn
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    CloseInput = false,
                    IgnoreComments = false,
                    IgnoreProcessingInstructions = false,
                    IgnoreWhitespace = false
                };

                var xml = new XmlDocument
                {
                    XmlResolver = null // chặn XXE
                };

                // Parse
                using (var reader = XmlReader.Create(input, settings))
                {
                    xml.Load(reader);
                }

                if (xml.DocumentElement is null)
                    throw new InvalidDataException("Invalid SVG: missing root element.");

                ct.ThrowIfCancellationRequested();

                // Chuẩn bị NS cho XPath
                var nsmgr = new XmlNamespaceManager(xml.NameTable);
                nsmgr.AddNamespace("svg", "http://www.w3.org/2000/svg");

                // 1) Loại bỏ các node nguy hiểm phổ biến
                RemoveNodes(xml, DangerousNodesXPath, nsmgr);

                // 2) Làm sạch attributes nguy hiểm (đệ quy toàn cây)
                RemoveDangerousAttributes(xml.DocumentElement);

                ct.ThrowIfCancellationRequested();

                // 3) Xuất lại XML (UTF-8 không BOM)
                var ms = new MemoryStream();
                using (var xw = XmlWriter.Create(ms, new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    Indent = false,
                    OmitXmlDeclaration = false,
                    CloseOutput = false
                }))
                {
                    xml.Save(xw);
                }
                ms.Position = 0;
                return Task.FromResult<Stream>(ms);
            }
            catch (XmlException ex)
            {
                // SVG không hợp lệ về cú pháp XML
                throw new InvalidDataException("Invalid SVG XML.", ex);
            }
        }

        private static void RemoveNodes(XmlDocument xml, string xpath, XmlNamespaceManager nsmgr)
        {
            var nodes = xml.SelectNodes(xpath, nsmgr);
            if (nodes == null || nodes.Count == 0) return;

            // Duyệt snapshot vì khi remove thay đổi collection
            var toRemove = new List<XmlNode>(nodes.Count);
            foreach (XmlNode n in nodes) toRemove.Add(n);

            foreach (var n in toRemove)
            {
                n.ParentNode?.RemoveChild(n);
            }
        }

        private static void RemoveDangerousAttributes(XmlElement root)
        {
            void CleanElement(XmlElement el)
            {
                var toRemove = new List<XmlAttribute>();

                foreach (XmlAttribute a in el.Attributes)
                {
                    var name = a.Name.ToLowerInvariant();
                    var val = a.Value?.Trim() ?? string.Empty;

                    // Loại bỏ toàn bộ sự kiện: onload, onclick, ...
                    if (name.StartsWith("on")) { toRemove.Add(a); continue; }

                    // Siết href/xlink:href: chỉ cho #fragment hoặc http/https
                    if (name == "href" || name.EndsWith(":href", StringComparison.Ordinal))
                    {
                        if (!IsSafeHref(val))
                        {
                            toRemove.Add(a);
                            continue;
                        }
                    }

                    // style inline: nếu chứa url()/behavior/-moz-binding thì bỏ
                    if (name == "style")
                    {
                        var v = val.ToLowerInvariant();
                        if (v.Contains("url(") || v.Contains("behavior") || v.Contains("-moz-binding"))
                        {
                            toRemove.Add(a);
                            continue;
                        }
                    }
                }

                foreach (var a in toRemove)
                    el.Attributes.Remove(a);

                // Đệ quy con
                foreach (XmlNode child in el.ChildNodes)
                {
                    if (child is XmlElement ce)
                        CleanElement(ce);
                }
            }

            CleanElement(root);
        }

        /// <summary>
        /// Chỉ cho phép:
        /// - "#fragment"
        /// - "http://..." hoặc "https://..."
        /// Không cho: javascript:, data:, file:, blob:, ftp:, mailto:, v.v.
        /// </summary>
        private static bool IsSafeHref(string href)
        {
            if (string.IsNullOrWhiteSpace(href)) return false;

            var v = href.Trim().ToLowerInvariant();

            if (v.StartsWith("#")) return true;
            if (v.StartsWith("http://") || v.StartsWith("https://")) return true;

            // Chặn tất cả schema khác (javascript:, data:, file:, blob:, ftp:, mailto:, …)
            return false;
        }
    }
}
