# FsAutoComplete Troubleshooting Guide

This guide covers common issues encountered when using FsAutoComplete (FSAC) and Ionide, along with their solutions.

## Table of Contents

- [Project Loading Issues](#project-loading-issues)
- [Performance Issues](#performance-issues)
- [Script File (.fsx) Issues](#script-file-fsx-issues)
- [Intellisense and Language Service Issues](#intellisense-and-language-service-issues)
- [Platform-Specific Issues](#platform-specific-issues)
- [Build and Compilation Issues](#build-and-compilation-issues)
- [FSI (F# Interactive) Issues](#fsi-f-interactive-issues)
- [Debugging and Logging](#debugging-and-logging)

---

## Project Loading Issues

### "Not in a F# project (Still loading...)" Message

**Symptoms:**
- Editor shows "Still loading..." message indefinitely
- No intellisense or language features available
- Project appears to be stuck during initial load

**Common Causes & Solutions:**

1. **Missing or Corrupted Project Restore**
   ```bash
   # Run a clean restore
   dotnet clean
   dotnet restore
   ```

2. **Invalid Project References**
   - Check all `<ProjectReference>` paths in your `.fsproj` files
   - Ensure referenced projects exist and can be built
   - Verify relative paths are correct

3. **Multiple .NET SDK Versions**
   - Check `global.json` for specific SDK version requirements
   - Ensure the required SDK version is installed:
     ```bash
     dotnet --list-sdks
     ```
   - Install the required SDK if missing

4. **Solution Filter Issues**
   - If using `.slnf` (solution filter) files, ensure all referenced projects exist
   - Try opening the full `.sln` file instead

5. **Workspace Folder Configuration**
   - Ensure you've opened the correct folder containing `.sln` or `.fsproj` files
   - Try using "F#: Load Project" command to manually select a project

**Related Issues:**
- ionide/ionide-vscode-fsharp#1697
- ionide/ionide-vscode-fsharp#1479
- ionide/ionide-vscode-fsharp#313

### Projects Load Successfully but No Intellisense

**Symptoms:**
- Projects appear loaded in Solution Explorer
- No errors shown, but intellisense doesn't work
- "The namespace or module is not defined" errors despite correct code

**Solutions:**

1. **Check Output Panel**
   - Open VSCode Output panel (View → Output)
   - Select "F# Language Service" from dropdown
   - Look for error messages or warnings

2. **Verify Dependencies**
   ```bash
   # For projects using Paket
   dotnet paket restore
   
   # For projects using NuGet
   dotnet restore
   ```

3. **Clear FSAC Cache**
   - Delete `.ionide` folder in your project root
   - Restart VSCode
   - Reload window (Ctrl/Cmd + Shift + P → "Developer: Reload Window")

4. **Check Target Framework**
   - Ensure your `.fsproj` specifies a valid `<TargetFramework>`
   - Verify the target framework SDK is installed
   - For multi-targeting projects, check all specified frameworks

**Related Issues:**
- ionide/ionide-vscode-fsharp#1840
- ionide/ionide-vscode-fsharp#924
- ionide/ionide-vscode-fsharp#1161

---

## Performance Issues

### High Memory Usage by FSAutoComplete Process

**Symptoms:**
- FSAutoComplete process consuming several GB of RAM
- Editor becomes sluggish or unresponsive
- System becomes slow when working with F# projects

**Solutions:**

1. **Disable Unused Features**
   In VSCode settings:
   ```json
   {
     "FSharp.enableReferenceCodeLens": false,
     "FSharp.inlayHints.enabled": false,
     "FSharp.unusedOpensAnalyzer": false,
     "FSharp.unusedDeclarationsAnalyzer": false,
     "FSharp.simplifyNameAnalyzer": false
   }
   ```

2. **Limit Projects in Solution**
   - Use solution filters (`.slnf`) to load only necessary projects
   - Consider breaking large solutions into smaller logical groups

3. **Adjust Code Lens Settings**
   ```json
   {
     "FSharp.enableReferenceCodeLens": false,
     "editor.codeLens": false
   }
   ```

4. **Restart FSAC Periodically**
   - Use "F#: Restart Language Service" command
   - Consider restarting VSCode for long sessions

5. **Check for Memory Leaks**
   - Update to the latest version of Ionide
   - Report persistent memory issues with details about your project structure

**Related Issues:**
- ionide/ionide-vscode-fsharp#1464
- ionide/ionide-vscode-fsharp#1752
- ionide/FsAutoComplete#1432

### Slow Project Loading

**Symptoms:**
- Projects take several minutes to load
- Sequential loading of many projects
- Editor unresponsive during load

**Solutions:**

1. **Use Solution Filters**
   Create a `.slnf` file to load only necessary projects:
   ```json
   {
     "solution": {
       "path": "YourSolution.sln",
       "projects": [
         "src\\ProjectA\\ProjectA.fsproj",
         "src\\ProjectB\\ProjectB.fsproj"
       ]
     }
   }
   ```

2. **Parallel Loading** (Available in newer versions)
   - Ensure you're using the latest version of Ionide
   - Project loading is now parallel by default

3. **Avoid Deep Project Dependencies**
   - Restructure projects to reduce dependency depth
   - Consider extracting shared code to packages

**Related Issues:**
- ionide/ionide-vscode-fsharp#1148
- ionide/ionide-vscode-fsharp#127

### Excessive Progress Notifications During Typing

**Symptoms:**
- Constant progress notifications appearing during typing
- UI feels janky or stuttering
- Frequent LSP progress updates in status bar

**Solutions:**

1. **Update to Latest Version**
   - This issue has been addressed in recent versions
   - Update Ionide and FsAutoComplete

2. **Adjust Debounce Settings**
   ```json
   {
     "FSharp.fsac.fsacDebounce": 500
   }
   ```

**Related Issues:**
- ionide/FsAutoComplete#1431

---

## Script File (.fsx) Issues

### No Intellisense in Script Files

**Symptoms:**
- `.fsx` files show no intellisense
- "File not parsed" errors in output
- Red squiggly lines under valid code

**Solutions:**

1. **Check FSI Extra Parameters**
   - Remove or adjust `FSharp.fsiExtraParameters` setting
   - Invalid parameters can break script parsing
   ```json
   {
     "FSharp.fsiExtraParameters": []
   }
   ```

2. **Script Framework Settings**
   - Ensure correct runtime is configured:
   ```json
   {
     "FSharp.fsac.dotnetArgs": [],
     "FSharp.dotNetRoot": ""
   }
   ```

3. **Wait for Initial Parse**
   - Script files may take a moment to parse initially
   - Check output panel for parsing progress

4. **Use Correct `#r` Syntax**
   For .NET 5+ scripts:
   ```fsharp
   #r "nuget: PackageName, Version"
   ```
   
   For older scripts:
   ```fsharp
   #r "nuget: PackageName"
   #load ".paket/load/main.group.fsx"
   ```

**Related Issues:**
- ionide/ionide-vscode-fsharp#1357
- ionide/ionide-vscode-fsharp#948
- ionide/ionide-vscode-fsharp#1244
- ionide/FsAutoComplete#1210

### `#r "nuget:"` Directives Not Working

**Symptoms:**
- NuGet package references in scripts not resolving
- Cannot find types from referenced packages
- Script hangs when using `#r "nuget:"`

**Solutions:**

1. **Check Internet Connection**
   - Package downloads require network access
   - Check firewall/proxy settings

2. **Clear NuGet Cache**
   ```bash
   dotnet nuget locals all --clear
   ```

3. **Use Explicit Versions**
   ```fsharp
   #r "nuget: PackageName, 1.0.0"
   ```

4. **Check Package Compatibility**
   - Ensure package supports .NET Standard 2.0 or your target framework
   - Some packages may not work in FSI context

5. **Timeout Issues**
   - Increase timeout if packages are large
   - Check FSAC output for download errors

**Related Issues:**
- ionide/FsAutoComplete#603
- ionide/FsAutoComplete#1247
- ionide/ionide-vscode-fsharp#1341

### Script Files with `fsi` Variable Issues

**Symptoms:**
- `fsi` variable not recognized in script files
- Missing FSI-specific functionality

**Solutions:**

1. **Use FSI Context Properly**
   ```fsharp
   // Check if running in FSI
   #if INTERACTIVE
   // FSI-specific code
   #else
   // Regular code
   #endif
   ```

2. **Update FsAutoComplete**
   - Older versions had issues with `fsi` variable
   - Update to latest version

**Related Issues:**
- ionide/FsAutoComplete#227

---

## Intellisense and Language Service Issues

### Intellisense Stops Working Randomly

**Symptoms:**
- Intellisense works initially but stops after editing
- No completions appear when typing
- Must restart VSCode to restore functionality

**Solutions:**

1. **Restart Language Service**
   - Command: "F#: Restart Language Service"
   - Keyboard shortcut available in command palette

2. **Check for Syntax Errors**
   - Fix any syntax errors in the file
   - Incomplete code can prevent intellisense

3. **Verify File is Saved**
   - Some features require saved files
   - Auto-save setting can help:
   ```json
   {
     "files.autoSave": "afterDelay"
   }
   ```

4. **Clear and Rebuild**
   ```bash
   dotnet clean
   dotnet build
   ```

5. **Check Extension Conflicts**
   - Disable other extensions temporarily
   - Known conflict with C# extension in some cases

**Related Issues:**
- ionide/ionide-vscode-fsharp#313
- ionide/ionide-vscode-fsharp#1568
- ionide/ionide-vscode-fsharp#1855

### Completion Items Not Showing Up

**Symptoms:**
- Typing `.` after object shows no members
- Must press trigger key multiple times
- Incomplete completion lists

**Solutions:**

1. **Manual Trigger**
   - Press `Ctrl+Space` (Windows/Linux) or `Cmd+Space` (Mac)
   - Try typing a few more characters

2. **Wait for Type Checking**
   - Large projects may need time to complete type checking
   - Check status bar for progress indicators

3. **Verify Project References**
   - Ensure referenced assemblies are present
   - Check project dependencies are built

4. **Adjust Trigger Settings**
   ```json
   {
     "editor.quickSuggestions": {
       "other": true,
       "comments": false,
       "strings": false
     }
   }
   ```

**Related Issues:**
- ionide/ionide-vscode-fsharp#1289

### Hover Tooltips Not Appearing

**Symptoms:**
- Hovering over symbols shows no tooltip
- Expected type information not displayed

**Solutions:**

1. **Check Hover Settings**
   ```json
   {
     "editor.hover.enabled": true,
     "editor.hover.delay": 300
   }
   ```

2. **Wait for Type Checking**
   - Tooltips require completed type checking
   - Check for errors in the file

3. **Verify Symbol is Defined**
   - Hover only works on recognized symbols
   - Red squiggly lines indicate issues

**Related Issues:**
- ionide/ionide-vscode-fsharp#313

### Rename Functionality Not Working

**Symptoms:**
- Rename command doesn't work or is grayed out
- Rename only changes current file
- Missing changes across project

**Solutions:**

1. **Use Correct Command**
   - Use "F2" or right-click → "Rename Symbol"
   - Ensure symbol is a valid rename target (not keyword)

2. **Wait for Project Load**
   - Rename across files requires fully loaded project
   - Check Solution Explorer shows all projects

3. **Check Symbol Scope**
   - Local symbols only rename in current file
   - Public symbols rename across project

4. **Known Limitations**
   - Some complex scenarios not yet supported
   - Consider using Find All References + manual edit

**Related Issues:**
- ionide/ionide-vscode-fsharp#1816

---

## Platform-Specific Issues

### macOS Issues

#### "Could not find 'dotnet'" Error

**Solutions:**

1. **Verify dotnet in PATH**
   ```bash
   which dotnet
   dotnet --version
   ```

2. **Add to PATH**
   Add to `~/.zshrc` or `~/.bash_profile`:
   ```bash
   export PATH="$PATH:/usr/local/share/dotnet"
   ```

3. **Restart VSCode**
   - Fully quit VSCode (Cmd+Q)
   - Relaunch to pick up PATH changes

4. **Specify dotnet Path**
   In VSCode settings:
   ```json
   {
     "FSharp.dotNetRoot": "/usr/local/share/dotnet"
   }
   ```

**Related Issues:**
- ionide/ionide-vscode-fsharp#2112
- ionide/ionide-vscode-fsharp#1949

#### Mac M1/ARM64 Issues

**Symptoms:**
- FSAC crashes on Apple Silicon Macs
- "Bad CPU type" errors
- Features not working on M1/M2/M3 Macs

**Solutions:**

1. **Use ARM64 Version of .NET**
   - Install ARM64 version of .NET SDK
   - Verify architecture:
   ```bash
   dotnet --info
   ```

2. **Update to Latest Versions**
   - Ensure latest Ionide and FsAutoComplete
   - Many ARM64 issues resolved in recent versions

3. **Check Rosetta 2**
   - Some older tools may need Rosetta 2
   - Install if not present:
   ```bash
   softwareupdate --install-rosetta
   ```

**Related Issues:**
- ionide/FsAutoComplete#939
- ionide/FsAutoComplete#1185

#### macOS Sonoma Issues

**Solutions:**

1. **Update All Tools**
   - Update to latest .NET SDK
   - Update VSCode to latest version
   - Update Ionide extension

2. **Permission Issues**
   - Grant VSCode necessary permissions in System Settings
   - Check Security & Privacy settings

**Related Issues:**
- ionide/FsAutoComplete#1185

### Linux Issues

#### FsAutoComplete Won't Start

**Symptoms:**
- "Failed to start language services" error
- FSAC process immediately exits

**Solutions:**

1. **Check Dependencies**
   ```bash
   # For Debian/Ubuntu
   sudo apt-get install libgdiplus libc6-dev
   
   # For Fedora/RHEL
   sudo dnf install libgdiplus glibc-devel
   ```

2. **Verify dotnet Installation**
   ```bash
   dotnet --version
   dotnet --info
   ```

3. **Check Permissions**
   ```bash
   # Ensure dotnet tools are executable
   chmod +x ~/.dotnet/tools/fsautocomplete
   ```

**Related Issues:**
- ionide/FsAutoComplete#871

#### Raspberry Pi / ARM Issues

**Symptoms:**
- FSAC fails to start on ARM devices
- Platform not supported errors

**Solutions:**

1. **Use Compatible .NET SDK**
   - Install ARM32 or ARM64 version as appropriate
   - Check compatibility with your device

2. **Build from Source**
   - May need to build FSAC from source for your platform
   - Follow contribution guidelines

**Related Issues:**
- ionide/FsAutoComplete#871

### Windows Issues

#### FSI Not Starting on Windows

**Symptoms:**
- `dotnet fsi` command not found
- FSI panel doesn't open

**Solutions:**

1. **Verify .NET SDK Installation**
   - `dotnet fsi` is included in the .NET SDK
   - Ensure the SDK is installed and `dotnet` is in your PATH
   ```bash
   dotnet --version
   ```

2. **Check PATH**
   - Ensure `dotnet` is accessible from your terminal
   - Restart terminal/VSCode after PATH changes

3. **Configure FSI Path (Optional)**
   If you need to use a specific FSI version (e.g., .NET Framework):
   ```json
   {
     "FSharp.fsiPath": "C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Community\\Common7\\IDE\\CommonExtensions\\Microsoft\\FSharp\\fsi.exe"
   }
   ```

**Related Issues:**
- ionide/ionide-vscode-fsharp#1087

---

## Build and Compilation Issues

### "Problem reading assembly" After Successful Build

**Symptoms:**
- `dotnet build` succeeds
- Intellisense shows errors about missing assemblies
- Cannot find types that definitely exist

**Solutions:**

1. **Clean and Rebuild**
   ```bash
   dotnet clean
   dotnet build
   ```

2. **Check Output Path**
   - Verify assemblies are in expected output directory
   - Check for multiple target frameworks

3. **Restart Language Service**
   - Command: "F#: Restart Language Service"
   - Ensures latest build artifacts are loaded

4. **Verify Project Configuration**
   - Check `.fsproj` for correct output paths
   - Ensure no conflicting build configurations

**Related Issues:**
- ionide/ionide-vscode-fsharp#924

### Missing FSharp.Core Reference

**Symptoms:**
- "Could not load file or assembly FSharp.Core"
- Basic F# types not recognized

**Solutions:**

1. **Add Explicit Reference**
   In `.fsproj`:
   ```xml
   <ItemGroup>
     <PackageReference Include="FSharp.Core" Version="8.0.0" />
   </ItemGroup>
   ```

2. **Check SDK Version**
   - Ensure F# SDK is included with .NET SDK
   - Reinstall .NET SDK if necessary

3. **Verify Target Framework**
   - Some frameworks require explicit FSharp.Core
   - Match FSharp.Core version to framework

**Related Issues:**
- ionide/FsAutoComplete#12
- ionide/FsAutoComplete#54

### Cross-Targeting Projects Issues

**Symptoms:**
- Multiple `<TargetFrameworks>` cause errors
- Intellisense confused about which framework is active

**Solutions:**

1. **Specify Primary Framework**
   In settings:
   ```json
   {
     "FSharp.preferredTargetFramework": "net8.0"
   }
   ```

2. **Use Solution Configurations**
   - Create separate configurations for different targets
   - Switch between them as needed

3. **Consider Separate Projects**
   - For very different frameworks, separate projects may be clearer

**Related Issues:**
- ionide/FsAutoComplete#162

---

## FSI (F# Interactive) Issues

### Sending Code to FSI is Very Slow

**Symptoms:**
- Sending selections to FSI takes several seconds
- Terminal becomes unresponsive
- Large delays between execution

**Solutions:**

1. **Check Terminal Settings**
   ```json
   {
     "terminal.integrated.enablePersistentSessions": false,
     "terminal.integrated.fastScrollSensitivity": 5
   }
   ```

2. **Use Smaller Code Selections**
   - Send smaller chunks instead of entire files
   - FSI performs better with incremental loading

3. **Consider Script Files**
   - Load entire files with `#load` instead of sending code
   ```fsharp
   #load "MyScript.fsx"
   ```

4. **VSCode Issue**
   - This may be a VSCode terminal limitation
   - Updates to VSCode may improve performance

**Related Issues:**
- ionide/ionide-vscode-fsharp#1412
- ionide/ionide-vscode-fsharp#1242

### FSI Extra Parameters Breaking Functionality

**Symptoms:**
- Error hints disappear in .fsx files
- FSI doesn't start or behaves incorrectly
- Script files stop working after adding parameters

**Solutions:**

1. **Validate Parameters**
   - Remove invalid FSI parameters
   - Check FSI documentation for valid options

2. **Reset to Defaults**
   ```json
   {
     "FSharp.fsiExtraParameters": []
   }
   ```

3. **Test Parameters Individually**
   - Add one parameter at a time
   - Verify functionality after each addition

**Related Issues:**
- ionide/FsAutoComplete#1210

---

## Debugging and Logging

### Collecting Diagnostic Information

When reporting issues, include:

1. **Version Information**
   ```bash
   dotnet --version
   dotnet --info
   code --version
   ```

2. **Extension Versions**
   - View → Extensions
   - Note Ionide-fsharp version

3. **Language Service Output**
   - View → Output
   - Select "F# Language Service"
   - Copy relevant error messages

4. **Enable Verbose Logging**
   In settings:
   ```json
   {
     "FSharp.fsac.verboseLogging": true,
     "FSharp.trace.server": "verbose"
   }
   ```

5. **Create Minimal Reproduction**
   - Simplify project to minimal failing case
   - Share as GitHub repository

### Using OpenTelemetry for Diagnostics

For performance investigation:

1. **Start Jaeger**
   ```bash
   docker run -d --name jaeger \
     -e COLLECTOR_OTLP_ENABLED=true \
     -p 16686:16686 \
     -p 4317:4317 \
     jaegertracing/all-in-one:latest
   ```

2. **Configure Environment**
   ```bash
   export OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"
   ```

3. **Enable in Settings**
   ```json
   {
     "FSharp.fsac.fsacArgs": ["--otel-exporter-enabled"]
   }
   ```

4. **View Traces**
   - Navigate to `http://localhost:16686/`
   - Analyze performance traces

### Common Log Messages

#### "File not parsed"
- File hasn't been loaded into language service
- Wait for initial parsing to complete
- Check for syntax errors

#### "Project parsing failed"
- Invalid project file
- Missing dependencies
- Check build succeeds: `dotnet build`

#### "Could not load project"
- Project file not found or corrupted
- Check paths and references

#### "Timeout"
- Operation took too long
- May indicate performance issue
- Check system resources

---

## Additional Resources

- [FsAutoComplete GitHub](https://github.com/ionide/FsAutoComplete)
- [Ionide Documentation](https://ionide.io/)
- [F# Language Reference](https://learn.microsoft.com/en-us/dotnet/fsharp/)
- [Report Issues](https://github.com/ionide/FsAutoComplete/issues)

## Getting Help

If you encounter an issue not covered here:

1. **Search Existing Issues**
   - Check FsAutoComplete, proj-info, and ionide-vscode-fsharp repositories
   - Someone may have reported the same issue

2. **Ask in Community**
   - [F# Slack](https://fsharp.org/guides/slack/) - #ionide channel
   - [F# Discord](https://discord.gg/fsharp)
   - Stack Overflow with `f#` and `ionide` tags

3. **Report New Issues**
   - Provide version information
   - Include minimal reproduction steps
   - Attach relevant logs
   - Describe expected vs actual behavior

## Contributing

If you've encountered and solved an issue not documented here, please contribute!
See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines.
