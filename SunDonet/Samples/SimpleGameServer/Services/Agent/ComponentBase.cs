using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bluebean.SimpleGameServer
{
    /// <summary>
    /// 组件拥有者接口 可以委托实现给ComponenetManager
    /// </summary>
    public interface IComponentOwner
    {
        /// <summary>
        /// 添加组件接口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T AddOwnerComponent<T>() where T : class, IComponent, new();
        /// <summary>
        /// 移除组件接口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void RemoveOwnerComponent<T>() where T : class, IComponent;
        /// <summary>
        /// 获取组件拥有者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetOwnerComponent<T>() where T : class, IComponent;
        /// <summary>
        /// 获取组件拥有者
        /// </summary>
        /// <param name="componentName"></param>
        /// <returns></returns>
        IComponent GetOwnerComponent(string componentName);
    }

    /// <summary>
    /// component组件
    /// </summary>
    /// <typeparam name="Owner"></typeparam>
    public interface IComponent
    {
        /// <summary>
        /// 获取组件名
        /// </summary>
        /// <returns></returns>
        string GetName();

        /// <summary>
        /// 注册完组件后调用
        /// </summary>
        void Init();

        /// <summary>
        /// 所有组件初始完调用
        /// </summary>
        void PostInit();

        /// <summary>
        /// 注销组件前调用
        /// </summary>
        void DeInit();

        /// <summary>
        /// 帧调用
        /// </summary>
        /// <param name="deltaMillisecond"></param>
        void Tick(UInt32 deltaMillisecond);

        /// <summary>
        /// 序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dest"></param>
        /// <returns>有数据写到dest返回true, 否则返回false</returns>
        bool Serialize<T>(T dest);

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        void DeSerialize<T>(T source);

        /// <summary>
        /// 所有组件反序列化完成后调用
        /// </summary>
        void PostDeSerialize();

        /// <summary>
        /// 拥有者玩家
        /// </summary>
        /// <returns></returns>
        IComponentOwner Owner { get; set; }
    }

    /// <summary>
    /// 管理component, 挂载在拥有者身上
    /// </summary>
    /// <typeparam name="Owner"></typeparam>
    public partial class ComponenetManager<ComponentType> where ComponentType : class, IComponent
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="owner"></param>
        public ComponenetManager(IComponentOwner owner)
        {
            m_owner = owner;
        }

        /// <summary>
        /// 添加组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Add<T>() where T : class, IComponent, new()
        {
            ComponentType component;
            ComponentType newComponent = new T() as ComponentType;
            string componentName = newComponent.GetName();
            if (m_components.TryGetValue(componentName, out component))
                return component as T;
            else
            {
                m_components.Add(componentName, newComponent);
                m_type2Name.Add(typeof(T), componentName);
                newComponent.Owner = m_owner;
                newComponent.Init();
                return newComponent as T;
            }
        }


        /// <summary>
        /// 移除组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Remove<T>() where T : class, IComponent
        {
            T component = GetComponent<T>();
            if (component != null)
            {
                OnRemove(component);
            }
        }

        /// <summary>
        /// 获取组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetComponent<T>() where T : class, IComponent
        {
            Type type = typeof(T);
            string componentName;
            ComponentType component;
            if (m_type2Name.TryGetValue(type, out componentName))
            {
                if (m_components.TryGetValue(componentName, out component))
                {
                    return component as T;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取组件
        /// </summary>
        /// <param name="componentName"></param>
        /// <returns></returns>
        public ComponentType GetComponent(string componentName)
        {
            ComponentType component;
            if (m_components.TryGetValue(componentName, out component))
            {
                return component;
            }

            return null;
        }
        /// <summary>
        /// 获取所有组件
        /// </summary>
        /// <returns></returns>
        public List<ComponentType> GetAllcomponents()
        {
            var componentList = new List<ComponentType>();
            foreach (var component in m_components)
            {
                componentList.Add(component.Value);
            }
            return componentList;

        }
        /// <summary>
        /// 序列化所有组件到dest
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dest"></param>
        /// <returns>返回true表示有数据序列化到dest, 否则为false</returns>
        public bool SerializeComponents<T>(T dest)
        {
            bool ret = false;

            foreach (var component in m_components)
            {
                ret |= component.Value.Serialize<T>(dest);
            }

            return ret;
        }

        /// <summary>
        /// 从source反序列化到组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        public void DeSerializeComponents<T>(T source)
        {
            foreach (var component in m_components)
            {
                component.Value.DeSerialize<T>(source);
            }
        }

        /// <summary>
        /// 所有组件反序列化完成后调用
        /// </summary>
        /// <param name="source"></param>
        public void PostDeSerializeComponents()
        {
            foreach (var component in m_components)
            {
                component.Value.PostDeSerialize();
            }
        }

        /// <summary>
        /// tick所有组件
        /// </summary>
        /// <param name="deltaMillisecond"></param>
        public void Tick(UInt32 deltaMillisecond)
        {
            foreach (var component in m_components)
            {
                component.Value.Tick(deltaMillisecond);
            }
        }

        /// <summary>
        /// 所有组件初始完调用
        /// </summary>
        public void PostInitComponents()
        {
            foreach (var component in m_components)
            {
                component.Value.PostInit();
            }
        }

        /// <summary>
        /// 移除所有已注册组件
        /// </summary>
        public void RemoveComponents()
        {
            var tempComponents = new List<ComponentType>(m_components.Values);
            foreach (var component in tempComponents)
            {
                OnRemove(component);
            }
        }

        /// <summary>
        /// 去除组件时调用
        /// </summary>
        /// <param name="component"></param>
        private void OnRemove(IComponent component)
        {
            component.DeInit();
            m_components.Remove(component.GetName());
            m_type2Name.Remove(component.GetType());
        }

        /// <summary>
        /// 组件容器
        /// </summary>
        private Dictionary<string, ComponentType> m_components = new Dictionary<string, ComponentType>();

        /// <summary>
        /// 类型对应名字
        /// </summary>
        private Dictionary<Type, string> m_type2Name = new Dictionary<Type, string>();

        /// <summary>
        /// 组件拥有者
        /// </summary>
        private IComponentOwner m_owner;
    }

}
