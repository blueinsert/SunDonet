
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SunDonet
{
    /// <summary>
    /// 类型实例的动态加载，配合dll的动态更新机制
    /// </summary>
    public class ClassLoader
    {
        private ClassLoader() { }

        public static ClassLoader CreateClassLoader()
        {
            if (m_instance == null)
            {
                m_instance = new ClassLoader();
            }
            return m_instance;
        }

        /// <summary>
        /// 创建指定类型的实例
        /// </summary>
        /// <param name="typeDNName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CreateInstance(TypeDNName typeDNName, params object[] args)
        {
            // 先加载类型
            var type = LoadType(typeDNName);
            if (type == null)
            {
                return null;
            }

            // 创建类型对象
            var obj = Activator.CreateInstance(type, args);

            // 创建类型对象
            return obj;
        }

        /// <summary>
        /// 加载指定的类型
        /// </summary>
        /// <param name="typeDNName"></param>
        /// <returns></returns>
        public Type LoadType(TypeDNName typeDNName)
        {
            Type type;
            bool ret = m_typeDict.TryGetValue(typeDNName.m_typeFullName, out type);
            if (!ret)
            {
                // 首先获取Assembly
                Assembly assembly;
                if (string.IsNullOrEmpty(typeDNName.m_assemblyName))
                {
                    assembly = m_assembleDict["Assembly-CSharp"];
                }
                else
                {
                    ret = m_assembleDict.TryGetValue(typeDNName.m_assemblyName, out assembly);
                    if (!ret)
                    {
                        return null;
                    }
                }

                // 从assembly获取类型
                type = assembly.GetType(typeDNName.m_typeFullName);
                if (type == null)
                {
                    assembly = m_assembleDict["Config"];
                    if (assembly == null)
                    {
                        Console.WriteLine(string.Format("Can not find type {0}, assemble [Config] is null", typeDNName.m_typeFullName));
                        return null;
                    }
                    type = assembly.GetType(typeDNName.m_typeFullName);
                    if (type == null)
                    {
                        Console.WriteLine(string.Format("Can not find type {0}", typeDNName.m_typeFullName));
                        return null;
                    }
                }
                m_typeDict[typeDNName.m_typeFullName] = type;
            }

            return type;
        }

        /// <summary>
        /// 加入assembly
        /// </summary>
        /// <param name="assembly"></param>
        public void AddAssembly(Assembly assembly)
        {
            m_assembleDict[assembly.GetName().Name] = assembly;
        }

        /// <summary>
        /// 单例访问器
        /// </summary>
        public static ClassLoader Instance { get { return m_instance; } }
        private static ClassLoader m_instance;

        /// <summary>
        /// assemble字典
        /// </summary>
        private Dictionary<string, Assembly> m_assembleDict = new Dictionary<string, Assembly>();

        /// <summary>
        /// 符号字典
        /// </summary>
        private Dictionary<string, Type> m_typeDict = new Dictionary<string, Type>();
    }

    /// <summary>
    /// 类型附带Assembly的完整路径例如 MyDLL@MyNamespace.MyClass
    /// </summary>
    [Serializable]
    
    public class TypeDNName
    {
        public TypeDNName(string typeDNName)
        {
            int index = typeDNName.IndexOf('@');
            if (index == -1)
            {
                // 不指定程序集的名字，使用通用的默认名字
                m_assemblyName = "Assembly-CSharp";
                m_typeFullName = typeDNName;
            }
            else
            {
                m_assemblyName = typeDNName.Substring(0, index);
                m_typeFullName = typeDNName.Substring(index + 1);
            }     
        }

        public override string ToString()
        {
            return string.Format("{0}@{1}", m_assemblyName, m_typeFullName);
        }

        /// <summary>
        /// 程序集名称
        /// </summary>
        public string m_assemblyName;

        /// <summary>
        /// 类型完整名称
        /// </summary>
        public string m_typeFullName;


    }
}
