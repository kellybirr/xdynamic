using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Dynamic.Xml
{
    /// <summary>
    /// Dynamic wrapper for XML used for DLR scripting
    /// </summary>
    public class XDynamic : DynamicObject, IComparable, IComparable<string>, IEnumerable<XDynamic>, IEquatable<string>, IConvertible
    {
        private readonly XElement _element;

        /// <summary>
        /// Initializes a new instance of the <see cref="XDynamic"/> class.
        /// </summary>
        /// <param name="element">The element.</param>
        public XDynamic(XElement element)
        {
            _element = element;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XDynamic"/> class.
        /// </summary>
        public XDynamic()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XDynamic"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public XDynamic(string name)
        {
            _element = new XElement(name);
        }

        /// <summary>
        /// Loads the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public static XDynamic Load(string uri)
        {
            return new XDynamic(XElement.Load(uri));
        }

        public static XDynamic Parse(string text)
        {
            return new XDynamic(XElement.Parse(text));
        }

        /// <summary>
        /// Saves the specified file name.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="options">The options.</param>
        /// <param name="encoding">The encoding to use.</param>
        public void Save(string fileName, SaveOptions options = SaveOptions.None, Encoding encoding = null)
        {
            File.WriteAllText(fileName, _element.ToString(options), encoding ?? Encoding.UTF8);
        }

        /// <summary>
        /// Saves the specified file name.
        /// </summary>
        /// <param name="writer">The TextWriter to write to.</param>
        /// <param name="options">The options.</param>
        public void Save(TextWriter writer, SaveOptions options = SaveOptions.None)
        {
            _element.Save(writer, options);
        }

        /// <summary>
        /// Saves the specified file name.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="options">The options.</param>
        public void Save(Stream stream, SaveOptions options = SaveOptions.None)
        {
            _element.Save(stream, options);
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public XDynamic Add(string name, object content = null)
        {
            var xNew = new XElement(name, content);
            _element.Add(xNew);

            return new XDynamic(xNew);
        }

        /// <summary>
        /// Provides the implementation for operations that set member values. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as setting a value for a property.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member to which the value is being assigned. For example, for the statement sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="value">The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, the <paramref name="value"/> is "Test".</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (binder.Name[0] == '_')
            {
                _element.SetAttributeValue(binder.Name.Substring(1).Replace("__", "-"), value);
            }
            else
            {
                XElement setNode = _element.Element(binder.Name);
                if (setNode != null)
                {
                    setNode.SetValue(value);
                }
                else
                {
                    var xNew = (value is XDynamic)
                        ? new XElement(binder.Name)
                        : new XElement(binder.Name, value);

                    _element.Add(xNew);
                }
            }

            return true;
        }

        /// <summary>
        /// Provides the implementation for operations that get member values. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as getting a value for a property.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="result">The result of the get operation. For example, if the method is called for a property, you can assign the property value to <paramref name="result"/>.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a run-time exception is thrown.)
        /// </returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (binder.Name[0] == '_')
            {
                XAttribute attr = _element.Attribute(binder.Name.Substring(1).Replace("__", "-"));
                if (attr != null)
                {
                    result = attr.Value;
                    return true;
                }
            }
            else
            {
                var query = from xChild in _element.Elements(binder.Name)
                            select new XDynamic(xChild);

                var resultList = query.ToList();
                if (resultList.Count > 0)
                {
                    result = (resultList.Count == 1) ? (object)resultList[0] : resultList;
                    return true;
                }
            }

            result = null;
            return true;
        }

        /// <summary>
        /// Provides implementation for type conversion operations. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations that convert an object from one type to another.
        /// </summary>
        /// <param name="binder">Provides information about the conversion operation. The binder.Type property provides the type to which the object must be converted. For example, for the statement (String)sampleObject in C# (CType(sampleObject, Type) in Visual Basic), where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Type returns the <see cref="T:System.String"/> type. The binder.Explicit property provides information about the kind of conversion that occurs. It returns true for explicit conversion and false for implicit conversion.</param>
        /// <param name="result">The result of the type conversion operation.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (binder.Type == typeof(String))
            {
                result = _element.Value;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation for operations that invoke a member. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as calling a method.
        /// </summary>
        /// <param name="binder">Provides information about the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleMethod". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="args">The arguments that are passed to the object member during the invoke operation. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <paramref name="args[0]"/> is equal to 100.</param>
        /// <param name="result">The result of the member invocation.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            Type xmlType = typeof(XElement);
            try
            {
            #if (NETSTANDARD1_5)
                MethodInfo method = xmlType.GetTypeInfo().GetDeclaredMethod(binder.Name);
                result = method.Invoke(_element, args);
            #else
                result = xmlType.InvokeMember(binder.Name, _invokeFlags, null, _element, args);
            #endif
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Binding flags for invoking members of
        /// </summary>
        private const BindingFlags _invokeFlags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance;


        /// <summary>
        /// Provides the implementation for operations that get a value by index. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for indexing operations.
        /// </summary>
        /// <param name="binder">Provides information about the operation.</param>
        /// <param name="indexes">The indexes that are used in the operation. For example, for the sampleObject[3] operation in C# (sampleObject(3) in Visual Basic), where sampleObject is derived from the DynamicObject class, <paramref name="indexes[0]"/> is equal to 3.</param>
        /// <param name="result">The result of the index operation.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a run-time exception is thrown.)
        /// </returns>
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (0 == (int)indexes[0])
            {
                result = this;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Gets the count of elements, always `1`, for compatibility with List(Of T).
        /// </summary>
        /// <value>The count.</value>
        public int Count
        {
            get { return 1; }
        }

        /// <summary>
        /// Gets the length of the string value.
        /// </summary>
        /// <value>The length.</value>
        public int Length
        {
            get { return _element.Value.Length; }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return _element.Value;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="XDynamic"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator string (XDynamic x)
        {
            return x._element.Value;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.Xml.Linq.XElement"/> to <see cref="XDynamic"/>.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator XDynamic(XElement element)
        {
            return new XDynamic(element);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="XDynamic"/> to <see cref="System.Xml.Linq.XElement"/>.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator XElement(XDynamic x)
        {
            return x._element;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public string Value
        {
            get { return _element.Value; }
        }

        /// <summary>
        /// Gets the value of the specified node name.
        /// </summary>
        /// <param name="nodeName">The name.</param>
        /// <returns></returns>
        public string Val(string nodeName)
        {
            var xx = _element.Element(nodeName);
            return (xx != null) ? xx.Value : null;
        }

        /// <summary>
        /// Gets the value of the node at the specified XPath
        /// </summary>
        /// <param name="xpath">the XPath to evaluate</param>
        /// <returns></returns>
        public string ValueOf(string xpath)
        {
            var xx = _element.XPathSelectElement(xpath);
            return (xx != null) ? xx.Value : null;
        }

        /// <summary>
        /// Compares to.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        int IComparable.CompareTo(object other)
        {
            return (_element.Value == null)
                ? ((other == null) ? 0 : 1)
                : _element.Value.CompareTo(other as string);
        }

        /// <summary>
        /// Compares to.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        int IComparable<string>.CompareTo(string other)
        {
            return (_element.Value == null)
                ? ((other == null) ? 0 : 1)
                : _element.Value.CompareTo(other);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is string) return Equals((string)obj);
            if (obj is int) return Equals((int)obj);

            return base.Equals(obj);
        }

        /// <summary>
        /// Equals the specified STR.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns></returns>
        public bool Equals(string str)
        {
            return (_element.Value == null)
                ? (str == null)
                : _element.Value.Equals(str);
        }

        /// <summary>
        /// Equals the specified other.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        public bool Equals(XDynamic other)
        {
            return Equals(other.Value);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return (_element != null ? _element.GetHashCode() : 0);
        }

#region IEnumerable<XDynamic> Members

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator<XDynamic> IEnumerable<XDynamic>.GetEnumerator()
        {
            yield return this;
        }

#endregion

#region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return this;
        }

#endregion

#region IConvertible Members

        TypeCode IConvertible.GetTypeCode()
        {
            return Type.GetTypeCode(GetType());
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(_element.Value, provider);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(_element.Value, provider);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return Convert.ToChar(_element.Value, provider);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            return Convert.ToDateTime(_element.Value, provider);
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(_element.Value, provider);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(_element.Value, provider);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(_element.Value, provider);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(_element.Value, provider);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(_element.Value, provider);
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(_element.Value, provider);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(_element.Value, provider);
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return Convert.ToString(_element.Value, provider);
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(_element.Value, conversionType, provider);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(_element.Value, provider);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(_element.Value, provider);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(_element.Value, provider);
        }

#endregion
    }
}
