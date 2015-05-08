using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Farbase
{
    class fbXmlLoader
    {
        private fbEngine engine;
        private fbInterface ui;
        public fbXmlLoader(fbInterface ui)
        {
            this.ui = ui;
            engine = ui.Engine;
        }

        public Widget WidgetFromXml(XmlNode xml)
        {
            Dictionary<string, XmlNode> nodes
                = new Dictionary<string, XmlNode>();

            foreach (XmlNode child in xml.ChildNodes)
            {
                if (child.Name == "#comment") continue;
                nodes.Add(child.Name, child);
            }

            Widget w;

            int internalPadding = 0;
            if (nodes.ContainsKey("internalpadding"))
            {
                internalPadding = Int32.Parse(
                    nodes["internalpadding"].InnerText
                    );

                nodes.Remove("internalpadding");
            }

            switch (xml.Name.ToLower())
            {
                case "button":
                    w = new Button(
                        nodes["content"].InnerText,
                        null,
                        engine,
                        ui
                    );

                    if (nodes.ContainsKey("keybind"))
                    {
                        ((Button)w).Subscribe(nodes["keybind"].InnerText);
                        nodes.Remove("keybind");
                    }

                    nodes.Remove("content");
                    break;

                case "checkbox":
                    w = new CheckBox(
                        engine,
                        ui
                    );

                    if (nodes.ContainsKey("keybind"))
                    {
                        ((CheckBox)w).Subscribe(nodes["keybind"].InnerText);
                        nodes.Remove("keybind");
                    }
                    break;

                case "image":
                    w = new Image(
                        nodes["content"].InnerText,
                        engine,
                        ui
                    );

                    nodes.Remove("content");
                    break;

                case "label":
                    w = new Label(
                        nodes["content"].InnerText,
                        engine,
                        ui
                    );

                    nodes.Remove("content");
                    break;

                case "texturebutton":
                    float scale = 1f;

                    if (nodes.ContainsKey("scale"))
                    {
                        scale = float.Parse(nodes["scale"].InnerText);
                        nodes.Remove("scale");
                    }

                    w = new TextureButton(
                        nodes["content"].InnerText,
                        null,
                        engine,
                        ui,
                        scale
                    );

                    if (nodes.ContainsKey("keybind"))
                    {
                        ((Button)w).Subscribe(nodes["keybind"].InnerText);
                        nodes.Remove("keybind");
                    }

                    nodes.Remove("content");
                    break;
                
                // === === === === === === === === === ===

                case "listbox":
                    w = new ListBox(
                        engine,
                        ui,
                        internalPadding
                    );
                    break;

                case "sidebyside":
                    w = new SideBySideWidgets(
                        engine,
                        ui,
                        internalPadding
                    );
                    break;

                default:
                    throw new ArgumentException();
            }

            if (nodes.ContainsKey("margins"))
            {
                int[] marginValues =
                    nodes["margins"].InnerText
                        .Split(',')
                        .Select(Int32.Parse)
                        .ToArray();

                switch (marginValues.Length)
                {
                    case 1:
                        w.Margins(
                            marginValues[0]
                        );
                        break;
                    case 2:
                        w.Margins(
                            marginValues[0],
                            marginValues[1]
                        );
                        break;
                    case 4:
                        w.Margins(
                            marginValues[0],
                            marginValues[1],
                            marginValues[2],
                            marginValues[3]
                        );
                        break;
                }

                nodes.Remove("margins");
            }

            if (nodes.ContainsKey("padding"))
            {
                int[] paddingValues =
                    nodes["padding"].InnerText
                        .Split(',')
                        .Select(Int32.Parse)
                        .ToArray();

                switch (paddingValues.Length)
                {
                    case 1:
                        w.Padding(
                            paddingValues[0]
                        );
                        break;
                    case 2:
                        w.Padding(
                            paddingValues[0],
                            paddingValues[1]
                        );
                        break;
                    case 4:
                        w.Padding(
                            paddingValues[0],
                            paddingValues[1],
                            paddingValues[2],
                            paddingValues[3]
                        );
                        break;
                }

                nodes.Remove("padding");
            }

            if (nodes.ContainsKey("alignment"))
            {
                string alignment = nodes["alignment"].InnerText.ToLower();
                string halign = alignment.Split(',')[0];
                string valign = alignment.Split(',')[1];

                Dictionary<string, HAlignment> hAlignments =
                    new Dictionary<string, HAlignment>
                {
                    { "left", HAlignment.Left },
                    { "center", HAlignment.Center },
                    { "right", HAlignment.Right }
                };

                Dictionary<string, VAlignment> vAlignments =
                    new Dictionary<string, VAlignment>
                {
                    { "top", VAlignment.Top },
                    { "center", VAlignment.Center },
                    { "bottom", VAlignment.Bottom }
                };

                w.SetAlign(hAlignments[halign], vAlignments[valign]);

                nodes.Remove("alignment");
            }

            if (nodes.ContainsKey("borderwidth"))
            {
                int width = Int32.Parse(nodes["borderwidth"].InnerText);
                w.SetBorder(width);

                nodes.Remove("borderwidth");
            }

            if (nodes.ContainsKey("backgroundalpha"))
            {
                float alpha = float.Parse(nodes["backgroundalpha"].InnerText);
                w.SetBackgroundAlpha(alpha);

                nodes.Remove("backgroundalpha");
            }

            if (nodes.ContainsKey("tooltip"))
            {
                w.SetTooltip(nodes["tooltip"].InnerText);

                nodes.Remove("tooltip");
            }

            if (nodes.ContainsKey("children"))
            {
                foreach (XmlNode child in nodes["children"].ChildNodes)
                {
                    ((ContainerWidget)w).AddChild(WidgetFromXml(child));
                }

                nodes.Remove("children");
            }

            if (nodes.ContainsKey("id"))
            {
                ui.NamedWidgets.Add(nodes["id"].InnerText.ToLower(), w);

                nodes.Remove("id");
            }

            //if we forgot to read some node
            //(because we want to read *EVERYTHING*
            if (nodes.Count > 0)
            {
                throw new FormatException();
            }

            return w;
        }

    }
}
