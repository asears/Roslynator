﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Roslynator.FindSymbols;

namespace Roslynator.Documentation
{
    internal abstract class SymbolDefinitionWriter : IDisposable
    {
        private bool _disposed;
        private SymbolDisplayFormat _namespaceFormat;
        private SymbolDisplayFormat _typeFormat;
        private SymbolDisplayFormat _memberFormat;
        private SymbolDisplayFormat _enumMemberFormat;
        private SymbolDisplayFormat _explicitInterfaceImplementationFormat;
        private SymbolDisplayFormat _attributeFormat;

        protected SymbolDefinitionWriter(
            SymbolFilterOptions filter = null,
            DefinitionListFormat format = null,
            SymbolDocumentationProvider documentationProvider = null)
        {
            Filter = filter ?? SymbolFilterOptions.Default;
            Format = format ?? DefinitionListFormat.Default;
            DocumentationProvider = documentationProvider;
        }

        public SymbolFilterOptions Filter { get; }

        public DefinitionListFormat Format { get; }

        public SymbolDocumentationProvider DocumentationProvider { get; }

        public SymbolDefinitionComparer Comparer
        {
            get
            {
                return (Format.Includes(SymbolDefinitionPartFilter.ContainingNamespace))
                    ? SymbolDefinitionComparer.SystemFirst
                    : SymbolDefinitionComparer.SystemFirstOmitContainingNamespace;
            }
        }

        public abstract bool SupportsDocumentationComments { get; }

        public virtual bool SupportsMultilineDefinitions => Layout != SymbolDefinitionListLayout.TypeHierarchy;

        protected int Depth { get; private set; }

        internal SymbolDefinitionListLayout Layout => Format.Layout;

        public SymbolDisplayFormat NamespaceFormat
        {
            get
            {
                if (_namespaceFormat == null)
                {
                    SymbolDisplayFormat format = SymbolDefinitionDisplayFormats.FullDefinition_NameAndContainingTypesAndNamespaces;

                    format = Format.Update(format);

                    Interlocked.CompareExchange(ref _namespaceFormat, CreateNamespaceFormat(format), null);
                }

                return _namespaceFormat;
            }
        }

        public SymbolDisplayFormat TypeFormat
        {
            get
            {
                if (_typeFormat == null)
                {
                    SymbolDisplayFormat format;

                    if (Layout == SymbolDefinitionListLayout.TypeHierarchy)
                    {
                        format = SymbolDefinitionDisplayFormats.HierarchyType;
                    }
                    else
                    {
                        format = (Format.Includes(SymbolDefinitionPartFilter.ContainingNamespace))
                            ? SymbolDefinitionDisplayFormats.FullDefinition_NameAndContainingTypesAndNamespaces
                            : SymbolDefinitionDisplayFormats.FullDefinition_NameOnly;
                    }

                    format = Format.Update(format);

                    Interlocked.CompareExchange(ref _typeFormat, CreateTypeFormat(format), null);
                }

                return _typeFormat;
            }
        }

        public SymbolDisplayFormat MemberFormat
        {
            get
            {
                if (_memberFormat == null)
                {
                    SymbolDisplayFormat format = (Format.Includes(SymbolDefinitionPartFilter.ContainingNamespace))
                        ? SymbolDefinitionDisplayFormats.FullDefinition_NameAndContainingTypesAndNamespaces
                        : SymbolDefinitionDisplayFormats.FullDefinition_NameAndContainingTypes;

                    format = Format.Update(format);

                    Interlocked.CompareExchange(ref _memberFormat, CreateMemberFormat(format), null);
                }

                return _memberFormat;
            }
        }

        public SymbolDisplayFormat ExplicitInterfaceImplementationFormat
        {
            get
            {
                if (_explicitInterfaceImplementationFormat == null)
                {
                    SymbolDisplayFormat format = MemberFormat.Update(memberOptions: MemberFormat.MemberOptions & ~SymbolDisplayMemberOptions.IncludeAccessibility);

                    Interlocked.CompareExchange(ref _explicitInterfaceImplementationFormat, format, null);
                }

                return _explicitInterfaceImplementationFormat;
            }
        }

        public SymbolDisplayFormat EnumMemberFormat
        {
            get
            {
                if (_enumMemberFormat == null)
                {
                    SymbolDisplayFormat format = SymbolDefinitionDisplayFormats.FullDefinition_NameOnly;

                    format = Format.Update(format);

                    Interlocked.CompareExchange(ref _enumMemberFormat, CreateEnumMemberFormat(format), null);
                }

                return _enumMemberFormat;
            }
        }

        public SymbolDisplayFormat AttributeFormat
        {
            get
            {
                if (_attributeFormat == null)
                {
                    SymbolDisplayFormat format = (Format.Includes(SymbolDefinitionPartFilter.ContainingNamespace))
                        ? SymbolDefinitionDisplayFormats.TypeNameAndContainingTypesAndNamespacesAndTypeParameters
                        : SymbolDefinitionDisplayFormats.TypeNameAndContainingTypesAndTypeParameters;

                    format = Format.Update(format);

                    Interlocked.CompareExchange(ref _attributeFormat, CreateAttributeFormat(format), null);
                }

                return _attributeFormat;
            }
        }

        protected void IncreaseDepth()
        {
            Depth++;
        }

        protected void DecreaseDepth()
        {
            if (Depth == 0)
                throw new InvalidOperationException("Cannot decrease depth.");

            Depth--;
        }

        public abstract void WriteStartDocument();

        public abstract void WriteEndDocument();

        public abstract void WriteStartAssemblies();

        public abstract void WriteEndAssemblies();

        public abstract void WriteStartAssembly(IAssemblySymbol assemblySymbol);

        public abstract void WriteAssemblyDefinition(IAssemblySymbol assemblySymbol);

        public abstract void WriteEndAssembly(IAssemblySymbol assemblySymbol);

        public abstract void WriteAssemblySeparator();

        public abstract void WriteStartNamespaces();

        public abstract void WriteEndNamespaces();

        public abstract void WriteStartNamespace(INamespaceSymbol namespaceSymbol);

        public abstract void WriteNamespaceDefinition(INamespaceSymbol namespaceSymbol, SymbolDisplayFormat format = null);

        public abstract void WriteEndNamespace(INamespaceSymbol namespaceSymbol);

        public abstract void WriteNamespaceSeparator();

        public abstract void WriteStartTypes();

        public abstract void WriteEndTypes();

        public abstract void WriteStartType(INamedTypeSymbol typeSymbol);

        public abstract void WriteTypeDefinition(INamedTypeSymbol typeSymbol, SymbolDisplayFormat format = null, SymbolDisplayTypeDeclarationOptions? typeDeclarationOptions = null);

        public abstract void WriteEndType(INamedTypeSymbol typeSymbol);

        public abstract void WriteTypeSeparator();

        public abstract void WriteStartMembers();

        public abstract void WriteEndMembers();

        public abstract void WriteStartMember(ISymbol symbol);

        public abstract void WriteMemberDefinition(ISymbol symbol, SymbolDisplayFormat format = null);

        public abstract void WriteEndMember(ISymbol symbol);

        public abstract void WriteMemberSeparator();

        public abstract void WriteStartEnumMembers();

        public abstract void WriteEndEnumMembers();

        public abstract void WriteStartEnumMember(ISymbol symbol);

        public abstract void WriteEnumMemberDefinition(ISymbol symbol, SymbolDisplayFormat format = null);

        public abstract void WriteEndEnumMember(ISymbol symbol);

        public abstract void WriteEnumMemberSeparator();

        public abstract void WriteStartAttributes(ISymbol symbol);

        public abstract void WriteEndAttributes(ISymbol symbol);

        public abstract void WriteStartAttribute(AttributeData attribute, ISymbol symbol);

        public abstract void WriteEndAttribute(AttributeData attribute, ISymbol symbol);

        public abstract void WriteAttributeSeparator(ISymbol symbol);

        public abstract void WriteDocumentationComment(ISymbol symbol);

        protected virtual SymbolDisplayTypeDeclarationOptions GetTypeDeclarationOptions()
        {
            var options = SymbolDisplayTypeDeclarationOptions.None;

            if (Format.Includes(SymbolDefinitionPartFilter.Accessibility))
                options |= SymbolDisplayTypeDeclarationOptions.IncludeAccessibility;

            if (Format.Includes(SymbolDefinitionPartFilter.Modifiers))
                options |= SymbolDisplayTypeDeclarationOptions.IncludeModifiers;

            if (Format.Includes(SymbolDefinitionPartFilter.BaseType))
                options |= SymbolDisplayTypeDeclarationOptions.BaseType;

            if (Format.Includes(SymbolDefinitionPartFilter.BaseInterfaces))
                options |= SymbolDisplayTypeDeclarationOptions.Interfaces;

            return options;
        }

        protected virtual SymbolDisplayAdditionalOptions GetAdditionalOptions()
        {
            var options = SymbolDisplayAdditionalOptions.None;

            if (!Format.Includes(SymbolDefinitionPartFilter.ContainingNamespace))
                options |= SymbolDisplayAdditionalOptions.OmitContainingNamespace;

            if (Format.Includes(SymbolDefinitionPartFilter.Attributes))
                options |= SymbolDisplayAdditionalOptions.IncludeParameterAttributes | SymbolDisplayAdditionalOptions.IncludeAccessorAttributes;

            if (Format.Includes(SymbolDefinitionPartFilter.AttributeArguments))
                options |= SymbolDisplayAdditionalOptions.IncludeAttributeArguments;

            if (SupportsMultilineDefinitions)
            {
                if (Format.Includes(SymbolDefinitionFormatOptions.BaseList))
                    options |= SymbolDisplayAdditionalOptions.FormatBaseList;

                if (Format.Includes(SymbolDefinitionFormatOptions.Constraints))
                    options |= SymbolDisplayAdditionalOptions.FormatConstraints;

                if (Format.Includes(SymbolDefinitionFormatOptions.Parameters))
                    options |= SymbolDisplayAdditionalOptions.FormatParameters;

                if (Format.Includes(SymbolDefinitionFormatOptions.Attributes))
                    options |= SymbolDisplayAdditionalOptions.FormatAttributes;
            }

            if (Format.OmitIEnumerable)
                options |= SymbolDisplayAdditionalOptions.OmitIEnumerable;

            if (Format.PreferDefaultLiteral)
                options |= SymbolDisplayAdditionalOptions.PreferDefaultLiteral;

            if (Format.Includes(SymbolDefinitionPartFilter.TrailingSemicolon))
                options |= SymbolDisplayAdditionalOptions.IncludeTrailingSemicolon;

            return options;
        }

        protected virtual SymbolDisplayFormat CreateNamespaceFormat(SymbolDisplayFormat format)
        {
            return format;
        }

        protected virtual SymbolDisplayFormat CreateTypeFormat(SymbolDisplayFormat format)
        {
            return format;
        }

        protected virtual SymbolDisplayFormat CreateMemberFormat(SymbolDisplayFormat format)
        {
            return format;
        }

        protected virtual SymbolDisplayFormat CreateEnumMemberFormat(SymbolDisplayFormat format)
        {
            return format;
        }

        protected virtual SymbolDisplayFormat CreateAttributeFormat(SymbolDisplayFormat format)
        {
            return format;
        }

        public virtual void WriteDocument(IEnumerable<IAssemblySymbol> assemblies, CancellationToken cancellationToken = default)
        {
            WriteStartDocument();
            WriteAssemblies(assemblies, cancellationToken);

            if (!Format.GroupByAssembly)
            {
                if (Layout == SymbolDefinitionListLayout.TypeHierarchy)
                {
                    WriteTypeHierarchy(assemblies.SelectMany(a => a.GetTypes(Filter.IsMatch)), cancellationToken);
                }
                else
                {
                    WriteNamespaces(new OneOrMany<IAssemblySymbol>(assemblies.ToImmutableArray()), cancellationToken);
                }
            }

            WriteEndDocument();
        }

        public virtual void WriteAssemblies(IEnumerable<IAssemblySymbol> assemblies, CancellationToken cancellationToken = default)
        {
            WriteStartAssemblies();

            using (IEnumerator<IAssemblySymbol> en = assemblies
                .OrderBy(f => f.Name)
                .ThenBy(f => f.Identity.Version)
                .GetEnumerator())
            {
                if (en.MoveNext())
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        IAssemblySymbol assembly = en.Current;

                        WriteStartAssembly(assembly);
                        WriteAssemblyDefinition(assembly);

                        if (Format.GroupByAssembly)
                        {
                            if (Layout == SymbolDefinitionListLayout.TypeHierarchy)
                            {
                                WriteTypeHierarchy(assembly.GetTypes(Filter.IsMatch), cancellationToken);
                            }
                            else
                            {
                                WriteNamespaces(new OneOrMany<IAssemblySymbol>(assembly), cancellationToken);
                            }
                        }

                        WriteEndAssembly(assembly);

                        if (en.MoveNext())
                        {
                            WriteAssemblySeparator();
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            WriteEndAssemblies();
        }

        private void WriteNamespaces(in OneOrMany<IAssemblySymbol> assemblies, CancellationToken cancellationToken = default)
        {
            Dictionary<INamespaceSymbol, IEnumerable<INamedTypeSymbol>> typesByNamespace = null;

            if (Filter.SymbolGroups == SymbolGroupFilter.None)
            {
                typesByNamespace = assemblies
                    .SelectMany(a => a.GetNamespaces(n => !n.IsGlobalNamespace && Filter.IsMatch(n)))
                    .Distinct(MetadataNameEqualityComparer<INamespaceSymbol>.Instance)
                    .ToDictionary(f => f, _ => ImmutableArray<INamedTypeSymbol>.Empty.AsEnumerable());
            }
            else
            {
                typesByNamespace = assemblies
                    .SelectMany(a => a.GetTypes(t => t.ContainingType == null && Filter.IsMatch(t)))
                    .GroupBy(t => t.ContainingNamespace, MetadataNameEqualityComparer<INamespaceSymbol>.Instance)
                    .Where(g => Filter.IsMatch(g.Key))
                    .OrderBy(g => g.Key, Comparer.NamespaceComparer)
                    .ToDictionary(f => f.Key, f => f.AsEnumerable());
            }

            WriteStartNamespaces();

            if (Layout == SymbolDefinitionListLayout.NamespaceHierarchy)
            {
                WriteNamespaceHierarchy(typesByNamespace, cancellationToken);
            }
            else
            {
                Debug.Assert(Layout == SymbolDefinitionListLayout.NamespaceList, Layout.ToString());

                WriteNamespaces(typesByNamespace, cancellationToken);
            }

            WriteEndNamespaces();
        }

        private void WriteTypeHierarchy(IEnumerable<INamedTypeSymbol> types, CancellationToken cancellationToken = default)
        {
            TypeHierarchy hierarchy = TypeHierarchy.Create(types, SymbolDefinitionComparer.SystemFirst.TypeComparer);

            WriteTypeHierarchy(hierarchy, cancellationToken);
        }

        private void WriteTypeHierarchy(TypeHierarchy hierarchy, CancellationToken cancellationToken = default)
        {
            WriteStartTypes();
            WriteTypeHierarchyItem(hierarchy.Root, cancellationToken);

            ImmutableArray<TypeHierarchyItem>.Enumerator en = hierarchy.Interfaces.GetEnumerator();

            if (en.MoveNext())
            {
                INamedTypeSymbol symbol = hierarchy.InterfaceRoot.Symbol;

                WriteStartType(symbol);
                WriteTypeDefinition(symbol);
                WriteStartHierarchyTypes();

                do
                {
                    WriteTypeHierarchyItem(en.Current, cancellationToken);
                }
                while (en.MoveNext());

                WriteEndHierarchyTypes();
                WriteEndType(symbol);
            }

            WriteEndTypes();
        }

        internal virtual void WriteTypeHierarchyItem(TypeHierarchyItem item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WriteStartType(item.Symbol);
            WriteTypeDefinition(item.Symbol);

            if (!item.IsExternal)
                WriteMembers(item.Symbol);

            if (item.HasChildren)
            {
                WriteStartHierarchyTypes();

                foreach (TypeHierarchyItem child in item.Children())
                {
                    WriteTypeSeparator();
                    WriteTypeHierarchyItem(child);
                }

                WriteEndHierarchyTypes();
            }

            WriteEndType(item.Symbol);
        }

        protected virtual void WriteStartHierarchyTypes()
        {
        }

        protected virtual void WriteEndHierarchyTypes()
        {
        }

        private void WriteNamespaces(Dictionary<INamespaceSymbol, IEnumerable<INamedTypeSymbol>> typesByNamespace, CancellationToken cancellationToken = default)
        {
            using (Dictionary<INamespaceSymbol, IEnumerable<INamedTypeSymbol>>.Enumerator en = typesByNamespace.GetEnumerator())
            {
                if (en.MoveNext())
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        INamespaceSymbol namespaceSymbol = en.Current.Key;

                        WriteStartNamespace(namespaceSymbol);
                        WriteNamespaceDefinition(namespaceSymbol);

                        if (Filter.Includes(SymbolGroupFilter.Type))
                            WriteTypes(en.Current.Value, cancellationToken);

                        WriteEndNamespace(namespaceSymbol);

                        if (en.MoveNext())
                        {
                            WriteNamespaceSeparator();
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void WriteNamespaceHierarchy(Dictionary<INamespaceSymbol, IEnumerable<INamedTypeSymbol>> typesByNamespace, CancellationToken cancellationToken = default)
        {
            var rootNamespaces = new HashSet<INamespaceSymbol>(MetadataNameEqualityComparer<INamespaceSymbol>.Instance);

            var nestedNamespaces = new HashSet<INamespaceSymbol>(MetadataNameEqualityComparer<INamespaceSymbol>.Instance);

            foreach (INamespaceSymbol namespaceSymbol in typesByNamespace.Select(f => f.Key))
            {
                if (namespaceSymbol.IsGlobalNamespace)
                {
                    rootNamespaces.Add(namespaceSymbol);
                }
                else
                {
                    INamespaceSymbol n = namespaceSymbol;

                    while (true)
                    {
                        INamespaceSymbol containingNamespace = n.ContainingNamespace;

                        if (containingNamespace.IsGlobalNamespace)
                        {
                            rootNamespaces.Add(n);
                            break;
                        }

                        nestedNamespaces.Add(n);

                        n = containingNamespace;
                    }
                }
            }

            using (IEnumerator<INamespaceSymbol> en = rootNamespaces
                .OrderBy(f => f, Comparer.NamespaceComparer)
                .GetEnumerator())
            {
                if (en.MoveNext())
                {
                    while (true)
                    {
                        WriteNamespaceWithHierarchy(en.Current);

                        if (en.MoveNext())
                        {
                            WriteNamespaceSeparator();
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            void WriteNamespaceWithHierarchy(INamespaceSymbol namespaceSymbol)
            {
                cancellationToken.ThrowIfCancellationRequested();

                WriteNamespaceDefinition(namespaceSymbol);

                if (Filter.Includes(SymbolGroupFilter.Type))
                    WriteTypes(typesByNamespace[namespaceSymbol], cancellationToken);

                using (List<INamespaceSymbol>.Enumerator en = nestedNamespaces
                    .Where(f => MetadataNameEqualityComparer<INamespaceSymbol>.Instance.Equals(f.ContainingNamespace, namespaceSymbol))
                    .Distinct(MetadataNameEqualityComparer<INamespaceSymbol>.Instance)
                    .OrderBy(f => f, Comparer.NamespaceComparer)
                    .ToList()
                    .GetEnumerator())
                {
                    if (en.MoveNext())
                    {
                        while (true)
                        {
                            nestedNamespaces.Remove(en.Current);

                            WriteNamespaceWithHierarchy(en.Current);

                            if (en.MoveNext())
                            {
                                WriteNamespaceSeparator();
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                WriteEndNamespace(namespaceSymbol);
            }
        }

        private void WriteTypes(IEnumerable<INamedTypeSymbol> types, CancellationToken cancellationToken = default)
        {
            using (IEnumerator<INamedTypeSymbol> en = types
                .OrderBy(f => f, Comparer.TypeComparer)
                .GetEnumerator())
            {
                if (en.MoveNext())
                {
                    WriteStartTypes();

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        INamedTypeSymbol type = en.Current;

                        WriteStartType(type);
                        WriteTypeDefinition(type);
                        WriteMembers(type);
                        WriteEndType(type);

                        if (en.MoveNext())
                        {
                            WriteTypeSeparator();
                        }
                        else
                        {
                            break;
                        }
                    }

                    WriteEndTypes();
                }
            }
        }

        internal void WriteMembers(INamedTypeSymbol type)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Class:
                case TypeKind.Interface:
                case TypeKind.Struct:
                    {
                        if (Filter.Includes(SymbolGroupFilter.Member))
                            WriteMembers();

                        if (Layout != SymbolDefinitionListLayout.TypeHierarchy
                            && (Filter.Includes(SymbolGroupFilter.Type)))
                        {
                            WriteTypes(type.GetTypeMembers().Where(f => Filter.IsMatch(f)));
                        }

                        break;
                    }
                case TypeKind.Enum:
                    {
                        if (Filter.Includes(SymbolGroupFilter.EnumField))
                            WriteEnumMembers();

                        break;
                    }
                default:
                    {
                        Debug.Assert(type.TypeKind == TypeKind.Delegate, type.TypeKind.ToString());
                        break;
                    }
            }

            void WriteMembers()
            {
                using (IEnumerator<ISymbol> en = type
                    .GetMembers()
                    .Where(f => !f.IsKind(SymbolKind.NamedType) && Filter.IsMatch(f))
                    .OrderBy(f => f, Comparer.MemberComparer)
                    .GetEnumerator())
                {
                    if (en.MoveNext())
                    {
                        WriteStartMembers();

                        ISymbol symbol = en.Current;

                        while (true)
                        {
                            WriteStartMember(symbol);
                            WriteMemberDefinition(symbol);
                            WriteEndMember(symbol);

                            if (en.MoveNext())
                            {
                                ISymbol next = en.Current;

                                if (Format.EmptyLineBetweenMembers
                                    || (Format.EmptyLineBetweenMemberGroups && symbol.GetMemberDeclarationKind() != next.GetMemberDeclarationKind()))
                                {
                                    WriteMemberSeparator();
                                }

                                symbol = next;
                            }
                            else
                            {
                                break;
                            }
                        }

                        WriteEndMembers();
                    }
                }
            }

            void WriteEnumMembers()
            {
                using (IEnumerator<ISymbol> en = type
                    .GetMembers()
                    .Where(m => m.Kind == SymbolKind.Field && Filter.IsMatch((IFieldSymbol)m))
                    .GetEnumerator())
                {
                    if (en.MoveNext())
                    {
                        WriteStartEnumMembers();

                        while (true)
                        {
                            WriteStartEnumMember(en.Current);
                            WriteEnumMemberDefinition(en.Current);
                            WriteEndEnumMember(en.Current);

                            if (en.MoveNext())
                            {
                                WriteEnumMemberSeparator();
                            }
                            else
                            {
                                break;
                            }
                        }

                        WriteEndEnumMembers();
                    }
                }
            }
        }

        public void WriteAttributes(ISymbol symbol)
        {
            using (IEnumerator<AttributeData> en = symbol
                .GetAttributes()
                .Where(f => Filter.IsMatch(symbol, f))
                .OrderBy(f => f.AttributeClass, Comparer.TypeComparer).GetEnumerator())
            {
                if (en.MoveNext())
                {
                    WriteStartAttributes(symbol);

                    while (true)
                    {
                        WriteStartAttribute(en.Current, symbol);
                        WriteAttribute(en.Current);
                        WriteEndAttribute(en.Current, symbol);

                        if (en.MoveNext())
                        {
                            WriteAttributeSeparator(symbol);
                        }
                        else
                        {
                            break;
                        }
                    }

                    WriteEndAttributes(symbol);
                }
            }
        }

        public virtual void WriteAttribute(AttributeData attribute)
        {
            WriteAttributeClass(attribute.AttributeClass);

            if (!Format.Includes(SymbolDefinitionPartFilter.AttributeArguments))
                return;

            bool hasConstructorArgument = false;
            bool hasNamedArgument = false;

            WriteConstructorArguments();
            WriteNamedArguments();

            if (hasConstructorArgument || hasNamedArgument)
            {
                Write(")");
            }

            void WriteConstructorArguments()
            {
                ImmutableArray<TypedConstant>.Enumerator en = attribute.ConstructorArguments.GetEnumerator();

                if (en.MoveNext())
                {
                    hasConstructorArgument = true;
                    Write("(");

                    while (true)
                    {
                        AddConstantValue(en.Current);

                        if (en.MoveNext())
                        {
                            Write(", ");
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            void WriteNamedArguments()
            {
                ImmutableArray<KeyValuePair<string, TypedConstant>>.Enumerator en = attribute.NamedArguments.GetEnumerator();

                if (en.MoveNext())
                {
                    hasNamedArgument = true;

                    if (hasConstructorArgument)
                    {
                        Write(", ");
                    }
                    else
                    {
                        Write("(");
                    }

                    while (true)
                    {
                        Write(en.Current.Key);
                        Write(" = ");
                        AddConstantValue(en.Current.Value);

                        if (en.MoveNext())
                        {
                            Write(", ");
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            void AddConstantValue(TypedConstant typedConstant)
            {
                switch (typedConstant.Kind)
                {
                    case TypedConstantKind.Primitive:
                        {
                            Write(SymbolDisplay.FormatPrimitive(typedConstant.Value, quoteStrings: true, useHexadecimalNumbers: false));
                            break;
                        }
                    case TypedConstantKind.Enum:
                        {
                            OneOrMany<EnumFieldSymbolInfo> oneOrMany = EnumUtility.GetConstituentFields(typedConstant.Value, (INamedTypeSymbol)typedConstant.Type);

                            OneOrMany<EnumFieldSymbolInfo>.Enumerator en = oneOrMany.GetEnumerator();

                            if (en.MoveNext())
                            {
                                while (true)
                                {
                                    WriteSymbol(en.Current.Symbol);

                                    if (en.MoveNext())
                                    {
                                        Write(" | ");
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                Write("(");
                                WriteSymbol((INamedTypeSymbol)typedConstant.Type);
                                Write(")");
                                Write(typedConstant.Value.ToString());
                            }

                            break;
                        }
                    case TypedConstantKind.Type:
                        {
                            Write("typeof");
                            Write("(");
                            WriteSymbol((ISymbol)typedConstant.Value);
                            Write(")");

                            break;
                        }
                    case TypedConstantKind.Array:
                        {
                            var arrayType = (IArrayTypeSymbol)typedConstant.Type;

                            Write("new ");
                            WriteSymbol(arrayType.ElementType);

                            Write("[] { ");

                            ImmutableArray<TypedConstant>.Enumerator en = typedConstant.Values.GetEnumerator();

                            if (en.MoveNext())
                            {
                                while (true)
                                {
                                    AddConstantValue(en.Current);

                                    if (en.MoveNext())
                                    {
                                        Write(", ");
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }

                            Write(" }");
                            break;
                        }
                    default:
                        {
                            throw new InvalidOperationException();
                        }
                }
            }

            void WriteSymbol(ISymbol symbol)
            {
                SymbolDisplayFormat format2 = (!Format.Includes(SymbolDefinitionPartFilter.ContainingNamespace))
                    ? SymbolDefinitionDisplayFormats.TypeNameAndContainingTypesAndTypeParameters
                    : SymbolDefinitionDisplayFormats.TypeNameAndContainingTypesAndNamespacesAndTypeParameters;

                Write(symbol.ToDisplayParts(format2));
            }
        }

        protected virtual void WriteAttributeClass(INamedTypeSymbol attributeClass, SymbolDisplayFormat format = null)
        {
            if (format == null)
            {
                format = (!Format.Includes(SymbolDefinitionPartFilter.ContainingNamespace))
                    ? SymbolDefinitionDisplayFormats.TypeNameAndContainingTypesAndTypeParameters
                    : SymbolDefinitionDisplayFormats.TypeNameAndContainingTypesAndNamespacesAndTypeParameters;
            }

            ImmutableArray<SymbolDisplayPart> parts = attributeClass.ToDisplayParts(format);

            parts = SymbolDefinitionWriterHelpers.RemoveAttributeSuffix(parts);

            Write(parts);
        }

        public virtual void Write(ISymbol symbol, SymbolDisplayFormat format, SymbolDisplayTypeDeclarationOptions? typeDeclarationOptions = null, SymbolDisplayAdditionalOptions? additionalOptions = null)
        {
            ImmutableArray<SymbolDisplayPart> parts = GetDisplayParts(symbol, format, typeDeclarationOptions, additionalOptions);

            Write(parts);
        }

        private protected ImmutableArray<SymbolDisplayPart> GetDisplayParts(
            ISymbol symbol,
            SymbolDisplayFormat format,
            SymbolDisplayTypeDeclarationOptions? typeDeclarationOptions,
            SymbolDisplayAdditionalOptions? additionalOptions)
        {
            return SymbolDefinitionDisplay.GetDisplayParts(
                symbol,
                format,
                typeDeclarationOptions: typeDeclarationOptions ?? GetTypeDeclarationOptions(),
                additionalOptions: additionalOptions ?? GetAdditionalOptions(),
                shouldDisplayAttribute: (s, a) => Filter.IsMatch(s, a));
        }

        public virtual void Write(ImmutableArray<SymbolDisplayPart> parts)
        {
            Write(parts, 0, parts.Length);
        }

        internal void Write(ImmutableArray<SymbolDisplayPart> parts, int start, int length)
        {
            int max = start + length;

            for (int i = start; i < max; i++)
                Write(parts[i]);
        }

        public virtual void Write(SymbolDisplayPart part)
        {
            Write(part.ToString());
        }

        public abstract void Write(string value);

        public abstract void WriteLine();

        public virtual void WriteLine(string value)
        {
            Write(value);
            WriteLine();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    Close();

                _disposed = true;
            }
        }

        public virtual void Close()
        {
        }
    }
}