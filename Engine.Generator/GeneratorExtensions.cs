﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Generator
{
	public static class GeneratorExtensions
	{
		internal static IEnumerable<INamedTypeSymbol> AllNestedTypesAndSelf(this INamedTypeSymbol type)
		{
			yield return type;
			foreach (var typeMember in type.GetTypeMembers())
			{
				foreach (var nestedType in typeMember.AllNestedTypesAndSelf())
				{
					yield return nestedType;
				}
			}
		}
		static IEnumerable<INamespaceSymbol> GetAllNamespaces(this INamespaceSymbol root)
		{
			yield return root;
			foreach (var child in root.GetNamespaceMembers())
				foreach (var next in GetAllNamespaces(child))
					yield return next;
		}

		public static IEnumerable<INamedTypeSymbol> GetTypeSymbols(Compilation compilation, Func<INamedTypeSymbol, bool> predicate)
		{
			foreach (var type in compilation.Assembly.GlobalNamespace.GetAllNamespaces().SelectMany(x => x.GetTypeMembers()).SelectMany(x => x.AllNestedTypesAndSelf()))
			{
				if (predicate(type))
					yield return type;
			}

			foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
			{
				foreach (var type in assembly.GlobalNamespace.GetAllNamespaces().SelectMany(x => x.GetTypeMembers()).SelectMany(x => x.AllNestedTypesAndSelf()))
				{
					if (predicate(type))
						yield return type;
				}
			}
		}

		public static IEnumerable<ISymbol> GetMemebers(INamedTypeSymbol symbol, Func<ISymbol, bool> predicate)
		{
			foreach (var member in symbol.GetMembers())
			{
				if (predicate(member))
					yield return member;
			}
		}

		public static string GetName(this NameSyntax name)
		{
			if (name is SimpleNameSyntax s)
			{
				return s.Identifier.Text;
			}
			else if (name is QualifiedNameSyntax q)
			{
				return q.Right.Identifier.Text;
			}

			throw new Exception("Unknown name type.");
		}

		public static string GetNamespace(this SyntaxNode syntax)
		{
			// If we don't have a namespace at all we'll return an empty string
			// This accounts for the "default namespace" case
			string nameSpace = string.Empty;

			// Get the containing syntax node for the type declaration
			// (could be a nested type, for example)
			SyntaxNode potentialNamespaceParent = syntax.Parent;

			// Keep moving "out" of nested classes etc until we get to a namespace
			// or until we run out of parents
			while (potentialNamespaceParent != null &&
					potentialNamespaceParent is not NamespaceDeclarationSyntax
					&& potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
			{
				potentialNamespaceParent = potentialNamespaceParent.Parent;
			}

			// Build up the final namespace by looping until we no longer have a namespace declaration
			if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
			{
				// We have a namespace. Use that as the type
				nameSpace = namespaceParent.Name.ToString();

				// Keep moving "out" of the namespace declarations until we 
				// run out of nested namespace declarations
				while (true)
				{
					if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
					{
						break;
					}

					// Add the outer namespace as a prefix to the final namespace
					nameSpace = $"{namespaceParent.Name}.{nameSpace}";
					namespaceParent = parent;
				}
			}

			// return the final namespace
			return nameSpace;
		}

		public static T FindNode<T>(this IEnumerable<SyntaxNode> nodes, Func<T, bool> predicate) where T : SyntaxNode
		{
			return nodes.Where(x => x is T).Cast<T>().Single(predicate);
		}

		public static bool TryFindNode<T>(this IEnumerable<SyntaxNode> nodes, Func<T, bool> predicate, out T node) where T : SyntaxNode
		{
			var filteredNodes = nodes.Where(x => x is T).Cast<T>();

			if (filteredNodes.Any(predicate))
			{
				node = filteredNodes.Single(predicate);
				return true;
			}
			else
			{
				node = default;
				return false;
			}
		}

		public static bool TryFindNode<T>(this IEnumerable<SyntaxNode> nodes, out T node) where T : SyntaxNode
		{
			var filteredNodes = nodes.Where(x => x is T).Cast<T>();

			if (filteredNodes.Any())
			{
				node = filteredNodes.Single();
				return true;
			}
			else
			{
				node = default;
				return false;
			}
		}
	}
}
