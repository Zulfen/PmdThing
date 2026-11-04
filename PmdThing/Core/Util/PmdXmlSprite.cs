using System.Xml.Serialization;

namespace PmdThing.Core.Util
{

    [Serializable]
    [XmlRoot("AnimData")]
    public class PmdXmlSprite
    {

        [XmlElement("ShadowSize")]
        public int ShadowSize { get; set; }

        [XmlArray("Anims")]
        [XmlArrayItem("Anim", typeof(Animation))]
        public Animation[] Animations { get; set; } = [];

        [Serializable]
        public class Animation
        {

            [XmlElement("Name")] 
            public string Name { get; set; }

            [XmlElement("FrameWidth")]
            public int Width { get; set; }

            [XmlElement("FrameHeight")]
            public int Height { get; set; }

            [XmlArray("Durations")]
            [XmlArrayItem("Duration", typeof(int))]
            public int[] Durations { get; set; } = [];

            [XmlElement("CopyOf")]
            public PmdConstants.AnimationType? CopyOf { get; set; }

        }
    }
}
