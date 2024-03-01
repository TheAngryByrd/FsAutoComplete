/// this file contains patches to the F# Compiler Service that have not yet made it into
/// published nuget packages.  We source-copy them here to have a consistent location for our to-be-removed extensions
module FsAutoComplete.FCSPatches

open FSharp.Compiler.Syntax
open FSharp.Compiler.Text
open FsAutoComplete.UntypedAstUtils
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.EditorServices

type LanguageFeatureShim =
  new: langFeature: string -> LanguageFeatureShim
  member Case: obj option
  static member Type: System.Type

type LanguageVersionShim =
  new: versionText: string -> LanguageVersionShim
  member IsPreviewEnabled: bool
  member SupportsFeature: featureId: LanguageFeatureShim -> bool

module LanguageVersionShim =
  val defaultLanguageVersion: Lazy<LanguageVersionShim>

  /// <summary>Tries to parse out "--langversion:" from OtherOptions if it can't find it, returns defaultLanguageVersion</summary>
  /// <returns>A LanguageVersionShim from the parsed "--langversion:" or defaultLanguageVersion </returns>
  val fromFSharpProject: snapshot: FSharpProjectSnapshot -> LanguageVersionShim

module SyntaxTreeOps =
  val synExprContainsError: SynExpr -> bool

type FSharpParseFileResults with

  member TryRangeOfNameOfNearestOuterBindingOrMember: pos: pos -> option<range * FSharpGlyph * LongIdent>
