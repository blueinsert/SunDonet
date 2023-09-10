using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SunDonet
{
    public class XmlAttributeOverridesItem
    {
        public XmlAttributeOverridesItem(Type ownerType, String xmlFieldName, String fieldName, Type replacementType)
        {
            OwnerType = ownerType;
            XmlFieldName = xmlFieldName;
            FieldName = fieldName;
            ReplacementType = replacementType;
        }

        public Type OwnerType { get; private set; }
        public String XmlFieldName { get; private set; }
        public String FieldName { get; private set; }
        public Type ReplacementType { get; private set; }
    }
    public class XmlLoader<T> where T : class
    {
        /// <summary>
        /// 配置信息对象，在调用Initialize后生效。
        /// </summary>
        public T Data { get; private set; }

        public static T Deserialize<T>(String xmlPath, XmlAttributeOverrides attrOverrides = null) where T : class
        {
            T tOut;

            /// 使用System.Xml.Serialization.XmlSerializer反序列化Xml文件
			XmlSerializer xmlSeliz = new XmlSerializer(typeof(T), attrOverrides ?? new XmlAttributeOverrides());
            /// 确保FileStream最终能够释放
            using (Stream xmlRd = new FileStream(xmlPath, FileMode.Open, FileAccess.Read))
            {
                tOut = xmlSeliz.Deserialize(xmlRd) as T;
            }

            return tOut;
        }

        public bool Initialize(String confPath)
        {
            try
            {
                XmlAttributeOverrides overrides = TryGetXmlAttributeOverrides();

                /// 使用辅助函数反序列化Configure对象
                Data = Deserialize<T>(confPath, overrides);
            }
            catch (Exception ex)
            {
                SunNet.Instance.Log.Info(ex.ToString());
                return false;
            }
            return true;
        }

        public static XmlAttributeOverrides TryGetXmlAttributeOverrides()
        {
            PropertyInfo prop;
            Type current = typeof(T);
            while ((prop = current.GetProperty("XmlOverrides")) == null && current.BaseType != typeof(Object))
            {
                current = current.BaseType;
            }
            XmlAttributeOverrides xOverrides = new XmlAttributeOverrides();
            if (prop != null)
            {
                foreach (XmlAttributeOverridesItem item in (IEnumerable<XmlAttributeOverridesItem>)prop.GetValue(null))
                {
                    xOverrides.ReplaceToDerived(item.OwnerType, item.XmlFieldName, item.FieldName, item.ReplacementType);
                }
            }
            return xOverrides;
        }

    }

    public static class XmlUtil
    {
        public static void ReplaceToDerived(this XmlAttributeOverrides overrides, Type ownerType,
                                            String xmlFieldName, String fieldName, Type replacementType)
        {
            // xmlFieldName字段当做replacementType来序列化
            XmlElementAttribute xElement = new XmlElementAttribute(xmlFieldName, replacementType);

            XmlAttributes attrs = new XmlAttributes();
            attrs.XmlElements.Add(xElement);

            // 这里第二个参数的字符串，应该是ownerType里字段原始名称，而不是XmlElement里定义的xmlFieldName名称。
            overrides.Add(ownerType, fieldName, attrs);
        }
    }
}
