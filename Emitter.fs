/// Emits .d.ts files from a data structure
module TreeType.Emitter

open TreeType.Util

let private indent (str: string) =
    str.Split "\n"
    |> Array.map (fun s -> "  " + s)
    |> String.concat "\n"

let private steriliseImport (import: string) =
    "_"
    + import
        .Replace('@', '_')
        .Replace('-', '_')
        .Replace('.', '_')
        .Replace('/', '_')

let propTemplate kind def = $"export %s{kind} %s{def};"

let refTemplate ref =
    $"""export * as %s{snd ref} from "%s{fst ref}";"""

let moduleTemplate name children =
    $"""declare module "%s{name}" {{
%s{indent children}
  export * as default from "%s{name}"
}}"""

let importAllTemplate import =
    $"""import * as %s{steriliseImport import} from "%s{import}";"""

let importTemplate import source =
    $"""import %s{import} from "%s{source}";"""

let reExportTemplate name source =
    $"""export * as %s{name} from "%s{source}";"""

let exportTemplate export = $"export {{ %s{export} }};"

let emitMember (mem: Member) =
    match mem.kind with
    | Import ->
        match mem.secondPart with
        | Some src -> importTemplate mem.typedef src
        | None -> importAllTemplate mem.typedef
    | Export ->
        match mem.secondPart with
        | Some src -> reExportTemplate mem.typedef src
        | None -> exportTemplate mem.typedef
    | _ -> propTemplate (string mem.kind) mem.typedef

let emitModule (nmsp: ContractedNamespace) =
    moduleTemplate
        nmsp.name
        (nmsp.children
         |> List.map (function
             | CRef ref -> refTemplate ref
             | CMem prop -> emitMember prop)
         |> String.concat "\n")

let emitAllModules =
    List.map emitModule >> String.concat "\n\n"

let emitImports =
    List.map importAllTemplate >> String.concat "\n"

let emitFull decls modules =
    let emittedDecls =
        Option.defaultValue "" decls

    let emittedModules = emitAllModules modules

    (emittedDecls + "\n\n" + emittedModules).Trim()