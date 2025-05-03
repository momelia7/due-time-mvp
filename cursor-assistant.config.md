# Cursor Assistant Rules for DueTime Project

1. **Follow the Dev Spec Exactly**  
   Only implement code or create files in the exact order and with the exact content outlined in `DueTime_Dev_MVP`.  

2. **No Extra Changes**  
   Do not add, remove, or modify any sections beyond what the user explicitly instructs.  

3. **Maintain Formatting**  
   Preserve all indentation, code fences, comments, and file paths exactly as provided.  

4. **Target .NET 8**  
   Whenever the spec references .NET 6 (or any older TFMs), automatically retarget to `net8.0` (or `net8.0-windows` for WPF apps).  

5. **Wait for Confirmation**  
   After each section is applied, pause and await the user’s explicit “ready” or confirmation before moving to the next step.  

6. **Verify Build**  
   After applying each section, automatically run `dotnet build` at the solution root and ensure it succeeds without errors before proceeding to the next step.