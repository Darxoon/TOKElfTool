using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ElfLib;
using ElfLib.Binary;

namespace TOKElfTool
{
    public class ObjectCollector<T>
    {
        private readonly ElfBinary<T> binary;
        private readonly List<bool> changedObjects;

        public ObjectCollector(ElfBinary<T> binary, List<bool> changedObjects)
        {
            this.binary = binary;
            this.changedObjects = changedObjects;
        }
        
        public void CollectObjects(UIElementCollection collection)
        {
            List<Element<T>> items = binary.Data[ElfType.Main];
            List<ObjectEditControl> controls = collection.OfType<ObjectEditControl>().ToList();
            
            for (int i = 0; i < items.Count; i++)
            {
                if (changedObjects[i] == true)
                    items[i].value = (T)CollectObject(controls[i], items[i].value.GetType());
            }
        }
        
        private object CollectObject(ObjectEditControl objectEditControl, Type type)
        {
            objectEditControl.Generate();

            UIElementCollection children = objectEditControl.Grid.Children;

            object currentObject = Activator.CreateInstance(type);

            // go through all property controls
            for (int i = 0; i < children.Count; i += 2)
            {
                // key label
                string propertyName = ((TextBlock)children[i]).Text;
                Type propertyType = type.GetField(propertyName).FieldType;

                // value
                object propertyValue = ReadFromControl(children[i + 1], propertyType);

                Trace.WriteLine($"{propertyName}: {propertyType.Name} = {propertyValue}");

                type.GetField(propertyName).SetValue(currentObject, propertyValue);

            }

            return currentObject;
        }
        
        private object ReadFromControl(UIElement child, Type propertyType)
        {
            // checkbox
            if (propertyType == typeof(bool))
            {
                CheckBox checkBox = (CheckBox)child;
                //Trace.WriteLine($"{propertyName}: {propertyType.Name} = {propertyValue} (from checkbox)");

                return checkBox.IsChecked;
            }

            // dropdown
            if (propertyType.BaseType == typeof(Enum))
            {
                ComboBox comboBox = (ComboBox)child;
                FieldInfo[] enumFields = propertyType.GetFields()
                    .Where(value => value.IsStatic)
                    .ToArray();

                FieldInfo selectedField = enumFields[comboBox.SelectedIndex];
                int selectedFieldValue = (int)selectedField.GetValue(null);

                //Trace.WriteLine($"~~~~~~~~~~ {propertyValue}");
                return selectedFieldValue;
            }

            // TextBox
            TextBox textBox = (TextBox)child;
            string text = textBox.Text;
            
            return propertyType.Name switch
            {
                nameof(String) => text.StartsWith("\"") && text.EndsWith("\"")
                    ? text.Substring(1, text.Length - 2)
                    : null,
                nameof(Vector3) => Vector3.FromString(text),
                nameof(Byte) => byte.Parse(text),
                nameof(Int32) => int.Parse(text),
                nameof(Int64) => long.Parse(text),
                nameof(Single) => float.Parse(text.EndsWith("f") ? text.Substring(0, text.Length - 1) : text),
                nameof(Double) => double.Parse(text),
                
                _ => throw new Exception("Couldn't read the property value"),
            };

        }
    }
}