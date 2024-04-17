using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace XmlToProto
{
	public class ProtoXml
	{
		private Dictionary<int, IMessage> _collection = new Dictionary<int, IMessage>();
		public IMessage findFile(Load.XmlInfo enemId)
		{
			int id = (int)enemId;
			if (_collection.ContainsKey(id))
			{
				return _collection[id];
			}
			return null;
		}

		public bool loadAllXml(int readFlag)
		{
			var cc = Load.LoadConfReflection.Descriptor.FindTypeByName<EnumDescriptor>("XmlInfo");
			var extension = Load.LoadConfExtensions.Loadset;
			foreach (EnumValueDescriptor valueDescriptor in cc.Values)
			{
				var ops = valueDescriptor.GetOptions();
				if (ops != null)
				{
					var fileHead = ops.GetExtension<Load.LoadSet>(extension);
					if (fileHead != null)
					{
						if (!loadXml(valueDescriptor.Number, fileHead.File, fileHead.Msg, readFlag))
						{
							return false;
						}
					}
				}
				MyLog.TestLog($"Name: {valueDescriptor.Name}, Number: {valueDescriptor.Number}");
			}
			return true;
		}

		public bool loadXml(int id, string fileName, string msgName, int read_flag = 0)
		{
			var msgDesc = Load.LoadConfReflection.Descriptor.FindTypeByName<MessageDescriptor>(msgName);
			if (msgDesc == null)
			{
				return false;
			}
			var message = msgDesc.Parser.ParseFrom(ByteString.Empty);
			string filePath = Environment.CurrentDirectory + "/../../../" + fileName;
			if (File.Exists(filePath))
			{
				XmlDocument xmlDoc = new XmlDocument();
				string xmlContent = File.ReadAllText(filePath);
				xmlDoc.LoadXml(xmlContent);

				XmlNode rootNode = xmlDoc.DocumentElement;
				if (!parseFromXml(message, rootNode, read_flag))
				{
					return false;
				}
			}
			_collection[id] = message;
			return true;
		}

		public bool parseFromXml(IMessage msg, XmlNode node,
		int readFlag)
		{
			var extension = Load.LoadConfExtensions.Confreadflag;
			var fieldsList = msg.Descriptor.Fields.InDeclarationOrder();
			foreach (FieldDescriptor field in fieldsList)
			{
				MyLog.TestLog($"Field Name: {field.Name}");
				MyLog.TestLog($"Field Number: {field.FieldNumber}");
				MyLog.TestLog($"Field Type: {field.FieldType}");

				bool canLoad = false;
				var ops = field.GetOptions();
				if (ops != null)
				{
					var value = ops.GetExtension<Load.ConfReadFlag>(extension);
					if (readFlag == (int)value || value == Load.ConfReadFlag.All)
					{
						canLoad = true;
					}
				}
				else
				{
					canLoad = true;
				}

				if (canLoad)
				{
					if (!parseFromXml(msg, field, node, readFlag))
					{
						return false;
					}
				}
			}
			return true;
		}

		public bool parseFromXml(IMessage msg, FieldDescriptor fieldDesc,
			XmlNode node, int readFlag)
		{
			XmlNode nodeAttr = null;
			if (fieldDesc.FieldType != FieldType.Message)
			{
				nodeAttr = node.Attributes.GetNamedItem(fieldDesc.Name);
				if (nodeAttr == null || string.IsNullOrEmpty(nodeAttr.InnerText))
				{
					return true;
				}
			}

			if (fieldDesc.IsRepeated)
			{
				return parseFromXmlArray(msg, fieldDesc, node, readFlag);
			}

			// 获取消息类型的反射信息  
			Type messageType = msg.GetType();
			// 根据字段名获取对应的属性  
			System.Reflection.PropertyInfo propertyInfo = messageType.GetProperty(fieldDesc.PropertyName);
			if (propertyInfo != null && propertyInfo.CanWrite)
			{
				string valueStr = "";
				if (nodeAttr != null) 
				{
					valueStr = nodeAttr.InnerText;
				}
				switch (fieldDesc.FieldType)
				{
					case FieldType.Int32:
						propertyInfo.SetValue(msg, Convert.ToInt32(valueStr));
						break;
					case FieldType.UInt32:
						propertyInfo.SetValue(msg, Convert.ToUInt32(valueStr));
						break;
					case FieldType.Int64:
						propertyInfo.SetValue(msg, Convert.ToInt64(valueStr));
						break;
					case FieldType.UInt64:
						propertyInfo.SetValue(msg, Convert.ToUInt64(valueStr));
						break;
					case FieldType.Double:
						propertyInfo.SetValue(msg, Convert.ToDouble(valueStr));
						break;
					case FieldType.Float:
						propertyInfo.SetValue(msg, float.Parse(valueStr));
						break;
					case FieldType.Bool:
						propertyInfo.SetValue(msg, Convert.ToBoolean(valueStr));
						break;
					case FieldType.Enum:
						propertyInfo.SetValue(msg, Convert.ToInt32(valueStr));
						break;
					case FieldType.String:
						propertyInfo.SetValue(msg, valueStr);
						break;
					case FieldType.Message:
					{
						var subNode = node.SelectSingleNode(fieldDesc.Name);
						if (subNode!=null)
						{
							IMessage subMsg = (IMessage)Activator.CreateInstance(fieldDesc.MessageType.ClrType);
							propertyInfo.SetValue(msg, subMsg);
							if (!parseFromXml(subMsg, subNode, readFlag))
							{
								return false;
							}
						}
					}
						break;
					default:
					{
						return false;
					}
				}
			}
			return true;
		}


		bool parseRepeatedFromString<T>(IMessage msg,
			FieldDescriptor fieldDesc,
			string input)
		{
			var arr = input.Split(',');
			if (arr.Length == 0)
			{
				return false;
			}

			var type = typeof(T);
			foreach (var value in arr)
			{
				repeatedContainerAddValue<T>(msg, fieldDesc.PropertyName, (T)Convert.ChangeType(value, type), typeof(T));
			}
			return true;
		}

		bool parseFromXmlArray(IMessage msg, FieldDescriptor fieldDesc,
			XmlNode node, int readFlag)
		{
			if (fieldDesc.FieldType == FieldType.Message)
			{
				var sub_node = node.SelectSingleNode(fieldDesc.Name);
				for (XmlNode n = sub_node; n != null; n = n.NextSibling)
				{
					IMessage subMsg = (IMessage)Activator.CreateInstance(fieldDesc.MessageType.ClrType);
					repeatedContainerAddValue<IMessage>(msg, fieldDesc.PropertyName, subMsg, subMsg.Descriptor.ClrType);
					if (!parseFromXml(subMsg, n, readFlag))
					{
						return false;
					}
				}
				return true;
			}

			string attrKeyName = fieldDesc.Name;
			var newNode = node.Attributes.GetNamedItem(fieldDesc.Name);
			switch (fieldDesc.FieldType)
			{
				case FieldType.Int32:
					{
						if (!parseRepeatedFromString<Int32>(msg, fieldDesc, newNode.InnerText))
						{
							MyLog.Log("load config failed: invalid format string of field [" + attrKeyName + ":" + newNode.InnerText + "]");
							return false;
						}
					}
					break;
				case FieldType.UInt32:
					{
						if (!parseRepeatedFromString<UInt32>(msg, fieldDesc, newNode.InnerText))
						{
							MyLog.Log("load config failed: invalid format string of field [" + attrKeyName + ":" + newNode.InnerText + "]");
							return false;
						}
					}
					break;
				case FieldType.Int64:
					{
						if (!parseRepeatedFromString<Int64>(msg, fieldDesc, newNode.InnerText))
						{
							MyLog.Log("load config failed: invalid format string of field [" + attrKeyName + ":" + newNode.InnerText + "]");
							return false;
						}
					}
					break;
				case FieldType.UInt64:
					{
						if (!parseRepeatedFromString<UInt64>(msg, fieldDesc, newNode.InnerText))
						{
							MyLog.Log("load config failed: invalid format string of field [" + attrKeyName + ":" + newNode.InnerText + "]");
							return false;
						}
					}
					break;
				case FieldType.Double:
					{
						if (!parseRepeatedFromString<Double>(msg, fieldDesc, newNode.InnerText))
						{
							MyLog.Log("load config failed: invalid format string of field [" + attrKeyName + ":" + newNode.InnerText + "]");
							return false;
						}
					}
					break;
				case FieldType.Float:
					{
						if (!parseRepeatedFromString<float>(msg, fieldDesc, newNode.InnerText))
						{
							MyLog.Log("load config failed: invalid format string of field [" + attrKeyName + ":" + newNode.InnerText + "]");
							return false;
						}
					}
					break;
				case FieldType.Bool:
					{
						if (!parseRepeatedFromString<Boolean>(msg, fieldDesc, newNode.InnerText))
						{
							MyLog.Log("load config failed: invalid format string of field [" + attrKeyName + ":" + newNode.InnerText + "]");
							return false;
						}
					}
					break;
				case FieldType.Enum:
					{
						if (!parseRepeatedFromString<Int32>(msg, fieldDesc, newNode.InnerText))
						{
							MyLog.Log("load config failed: invalid format string of field [" + attrKeyName + ":" + newNode.InnerText + "]");
							return false;
						}
					}
					break;
				case FieldType.String:
					{
						if (!parseRepeatedFromString<string>(msg, fieldDesc, newNode.InnerText))
						{
							MyLog.Log("load config failed: invalid format string of field [" + attrKeyName + ":" + newNode.InnerText + "]");
							return false;
						}
					}
					break;
				default:
					{
						MyLog.Log("load config failed: not supported protobuf data type" + fieldDesc.Name + " when translate message " + msg.Descriptor.Name);
						return false;
					}
			}
			return true;
		}

		private void repeatedContainerAddValue<T>(IMessage message, string attrName, T subValue, Type subType)
		{
			// 获取RepeatedField<IMessage>的属性  
			var repeatedFieldProperty = message.GetType().GetProperty(attrName);
			if (repeatedFieldProperty == null)
			{
				throw new ArgumentException("Field does not exist on the message.");
			}

			// 获取RepeatedField<IMessage>的实例  
			var repeatedField = repeatedFieldProperty.GetValue(message);
			if (repeatedField == null)
			{
				throw new InvalidOperationException("RepeatedField is null. You may need to initialize it first.");
			}

			// 调用RepeatedField的Add方法添加子消息
			MethodInfo addMethod = repeatedField.GetType().GetMethod("Add", new Type[] { subType });
			if (addMethod == null)
			{
				throw new MissingMethodException("RepeatedField does not have an Add method.");
			}
			addMethod.Invoke(repeatedField, new object[] { subValue });
		}
	};
}
