﻿using System;
using System.Collections.Generic;
using System.Text;

namespace XIV.Core.Utils
{
    public class ClassGenerator
    {
        class Builder
        {
            public StringBuilder builder = new StringBuilder();
            public int intendNumber;
        }

        public readonly string className;
        public readonly string classModifier;
        public readonly string accessModifier;
        public readonly bool isInnerClass;

        Builder classBuilder;
        Builder memberBuilder;
        Builder methodBuilder;
        Builder attributeBuilder;
        Builder innerClassBuilder;
        List<string> usings;
        List<string> namespaces;

        string GENERATION_TEXT => "/*" + Environment.NewLine + 
                                  "* Generated by " + typeof(ClassGenerator).Namespace + "." + nameof(ClassGenerator) + Environment.NewLine +
                                  "*/";

        public ClassGenerator(string className, string classModifier = "", string accessModifier = "public", string inheritance = "", bool isInnerClass = false)
        {
            InitializeBuilders();
            usings = new List<string>();
            namespaces = new List<string>();
            this.className = className;
            this.classModifier = classModifier;
            this.accessModifier = accessModifier;
            this.isInnerClass = isInnerClass;
            var line = Space(accessModifier) + Space(classModifier) + "class " + className;
            if (IsNull(inheritance) == false)
            {
                line += ": " + inheritance;
            }

            WriteLine(classBuilder, line);
        }

        public ClassGenerator(string className, string classDefinitionLine, bool isInnerClass = false)
        {
            InitializeBuilders();
            this.className = className;
            this.isInnerClass = isInnerClass;
            WriteLine(classBuilder, classDefinitionLine);
        }

        public void PutInsideNamespace(string @namespace)
        {
            if (namespaces.Contains(@namespace)) return;
            namespaces.Add(@namespace);
        }

        public void AddUsing(string libName)
        {
            if (usings.Contains(libName)) return;
            usings.Add(libName);
        }

        public void AddAttribute(string attribute)
        {
            attribute = attribute.TrimStart('[').TrimEnd(']');
            WriteLine(attributeBuilder, "[" + attribute + "]");
        }

        public void AddInnerClass(ClassGenerator classGenerator)
        {
            int usingsCount = classGenerator.usings.Count;
            for (int i = 0; i < usingsCount; i++)
            {
                AddUsing(classGenerator.usings[i]);
            }
            classGenerator.usings.Clear();
            WriteLine(innerClassBuilder, classGenerator.EndClass());
        }
        
        public void AddField(string fieldName, string fieldValue, string returnType, string modifier = "", string accessModifier = "public")
        {
            var line = Space(accessModifier) + Space(modifier) + Space(returnType) + Space(fieldName) + "=" + Space(fieldValue, false);
            WriteLine(memberBuilder, line + ";");
        }
        
        public void AddField(string line)
        {
            WriteLine(memberBuilder, line.TrimEnd(';') + ";");
        }
        
        public void AddGetOnlyProperty(string propertyName, string returnType, string getBlockContent, string modifier = "", string accessModifier = "public")
        {
            var line = Space(accessModifier) + Space(modifier) + Space(returnType) + propertyName;
            AddGetOnlyProperty(line, getBlockContent);
        }

        public void AddGetSetProperty(string propertyName, string returnType, string getBlockContent, string setBlockContent, string modifier = "", string accessModifier = "public")
        {
            var line = Space(accessModifier) + Space(modifier) + Space(returnType) + propertyName;
            AddGetSetProperty(line, getBlockContent, setBlockContent);
        }

        public void AddGetOnlyProperty(string line, string getBlockContent)
        {
            if (memberBuilder.builder.Length > 0) WriteLine(memberBuilder);
            WriteLine(memberBuilder, line);
            OpenBrackets(memberBuilder);
            WritePropertyBlock("get", getBlockContent);
            CloseBrackets(memberBuilder);
        }
        
        public void AddGetSetProperty(string propertyDefinition, string getBlockContent, string setBlockContent)
        {
            if (memberBuilder.builder.Length > 0) WriteLine(memberBuilder);
            WriteLine(memberBuilder, propertyDefinition);

            OpenBrackets(memberBuilder);
            WritePropertyBlock("get", getBlockContent);
            WritePropertyBlock("set", setBlockContent);
            CloseBrackets(memberBuilder);
        }

        public void StartMethod(string line)
        {
            WriteLine(methodBuilder, line);
            OpenBrackets(methodBuilder);
        }

        public void WriteMethodLine(string line)
        {
            WriteLine(methodBuilder, line.TrimEnd(';') + ";");
        }

        public void WriteCommentInsideMethod(string comment, bool oneLine = true)
        {
            AddComment(comment, methodBuilder, oneLine);
        }

        public void EndMethod()
        {
            CloseBrackets(methodBuilder);
        }

        public string EndClass()
        {
            StringBuilder code = new StringBuilder(512);

            if (isInnerClass == false) code.AppendLine(GENERATION_TEXT);
            
            WriteUsings(code);
            FillClassBody();

            if (attributeBuilder.builder.Length > 0)
            {
                classBuilder.builder.Insert(0, attributeBuilder.builder.ToString());
            }

            code.Append(PutInsideNamespace());
            ReleaseResources();
            return code.ToString();
        }

        void WriteUsings(StringBuilder code)
        {
            if (usings.Count == 0) return;
            for (var i = 0; i < usings.Count; i++)
            {
                code.AppendLine("using " + usings[i] + ";");
            }
            code.AppendLine();
        }

        void InitializeBuilders()
        {
            classBuilder = new Builder();
            memberBuilder = new Builder();
            methodBuilder = new Builder();
            attributeBuilder = new Builder();
            innerClassBuilder = new Builder();
        }

        void WritePropertyBlock(string blockType, string blockContent)
        {
            WriteLine(memberBuilder, blockType);
            OpenBrackets(memberBuilder);
            WriteLine(memberBuilder, blockContent);
            CloseBrackets(memberBuilder);
        }

        void FillClassBody()
        {
            void WriteInsideClass(Builder builder, string comment)
            {
                if (builder.builder.Length == 0) return;
                AddComment(comment, classBuilder, true);
                WriteLineByLine(classBuilder, builder.builder.ToString());
            }
            OpenBrackets(classBuilder);
            WriteInsideClass(innerClassBuilder, "Inner Classes");
            WriteInsideClass(memberBuilder, "Members");
            WriteInsideClass(methodBuilder, "Functions");
            CloseBrackets(classBuilder, "class " + className);
        }

        string PutInsideNamespace()
        {
            int count = namespaces.Count;
            if (isInnerClass || count == 0) return classBuilder.builder.ToString();

            Builder builder = new Builder();
            for (var i = 0; i < count; i++)
            {
                WriteLine(builder, "namespace " + namespaces[i]);
                OpenBrackets(builder);
            }
            
            WriteLineByLine(builder, classBuilder.builder.ToString());
            
            for (int i = count - 1; i >= 0; i--)
            {
                CloseBrackets(builder, namespaces[i]);
            }

            return builder.builder.ToString();
        }

        void AddComment(string comment, Builder builder, bool oneLine)
        {
            if (oneLine)
            {
                WriteLine(builder, "// " + comment);
                return;
            }

            comment = comment.Replace(Environment.NewLine, Environment.NewLine + "* ");
            WriteLine(builder, "/*");
            WriteLineByLine(builder, comment);
            WriteLine(builder, "*/");
        }

        void WriteLineByLine(Builder builder, string text)
        {
            if (text.Length == 0) return;
            string intend = GetIntend(builder);
            string[] splitted = text.Split(Environment.NewLine);
            int length = splitted.Length;
            for (int i = 0; i < length; i++)
            {
                builder.builder.AppendLine(intend + splitted[i]);
            }
        }

        void WriteLine(Builder builder, string line = "")
        {
            string intend = GetIntend(builder);
            builder.builder.AppendLine(intend + line);
        }

        void OpenBrackets(Builder builder, string bracesLineComment = "")
        {
            var braceLine = "{";
            if (IsNull(bracesLineComment) == false)
            {
                braceLine += "// " + bracesLineComment;
            }
            WriteLine(builder, braceLine);
            builder.intendNumber++;
        }

        void CloseBrackets(Builder builder, string bracesLineComment = "")
        {
            builder.intendNumber--;
            var braceLine = "}";
            if (IsNull(bracesLineComment) == false)
            {
                braceLine += " // " + bracesLineComment;
            }
            WriteLine(builder, braceLine);
        }

        void ReleaseResources()
        {
            classBuilder = null;
            methodBuilder = null;
            attributeBuilder = null;
            usings = null;
            namespaces = null;
        }

        static string Space(string value, bool after = true)
        {
            if (string.IsNullOrEmpty(value)) return "";

            return after ? value + " " : " " + value;
        }

        static bool IsNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) || string.IsNullOrEmpty(value);
        }

        static string GetIntend(Builder builder)
        {
            string intend = "";
            for (int i = 0; i < builder.intendNumber; i++)
            {
                intend += "\t";
            }

            return intend;
        }

        public override string ToString()
        {
            return EndClass();
        }
    }
}