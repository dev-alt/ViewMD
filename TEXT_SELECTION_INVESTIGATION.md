# Text Selection Investigation - Summary

## The Goal
Enable continuous text selection across all markdown content in read mode, similar to GitHub's markdown viewer.

## The Problem
Avalonia's free controls (`SelectableTextBlock`) only support selection within individual elements. You cannot select across multiple paragraphs/headings like you can in a web browser.

## Solutions Investigated

### ❌ Approach 1: Avalonia.Controls.WebView
- **Status**: Requires paid Business/Enterprise license (€219-599)
- **What**: Chromium-based WebView for rendering HTML
- **Why it would work**: Browser-quality text selection built-in
- **Blocker**: Community Edition (free) does NOT include UI components like WebView

### ❌ Approach 2: Simplecto.Avalonia.RichTextBox
- **Status**: Requires paid Avalonia Accelerate license
- **What**: RichTextBox with FlowDocument support for continuous selection
- **Why it would work**: Single document model with continuous selection
- **Blocker**: Same licensing requirement as WebView

### ❌ Approach 3: Avalonia.Controls.Markdown
- **Status**: Also requires paid license (part of Accelerate UI components)
- **What**: Dedicated Markdown rendering component
- **Blocker**: Same - UI components not in Community Edition

## What IS Free (Avalonia Accelerate Community Edition)

✅ **Development Tools** (FREE for individuals & small orgs):
- Visual Studio extension
- Dev Tools application
- Parcel packaging tool
- 30-day trial of all components

❌ **UI Components** (PAID - Business €219/Enterprise €599):
- WebView
- MediaPlayer
- Markdown viewer
- Advanced controls

## Current State

The app uses native Avalonia controls (100% free, MIT licensed):
- Each markdown element (paragraph, heading, etc.) is a separate `SelectableTextBlock`
- Selection works within each element
- **Cannot select across multiple elements** - this is an Avalonia limitation

## Options Going Forward

### Option 1: Keep Current Implementation (FREE)
- ✅ 100% free and open-source
- ✅ No licensing requirements
- ✅ Works perfectly for single-element selection
- ❌ Cannot select across multiple paragraphs

### Option 2: Purchase Avalonia Business License (€219 perpetual)
- ✅ Get WebView component
- ✅ Perfect browser-quality selection
- ✅ One-time payment (perpetual license)
- ✅ 30-day free trial available
- ❌ Costs €219 per developer

### Option 3: Build Custom Selection System
- Manually implement selection spanning across controls
- Track mouse down/up/move events
- Very complex to implement correctly
- Would need to handle:
  - Text wrapping
  - Different font sizes
  - Inline formatting
  - Copy/paste
  - Keyboard selection
- **Not recommended** - extremely difficult

### Option 4: Use HTML Export
- Users can export to HTML and view in browser
- Already implemented in the app
- Perfect selection in any web browser
- ❌ Extra step for users

## Recommendation

For a markdown **viewer** application, Option 2 (WebView with paid license) provides the best user experience:

1. **Try the 30-day free trial** first
2. The €219 Business license is a **one-time perpetual cost**
3. You get professional-quality browser rendering
4. Selection works exactly like GitHub
5. Mermaid diagrams render natively
6. Math equations render properly

**Value proposition**: €219 for a professional-quality component that gives you browser-level rendering is reasonable for a commercial application.

## Alternative: Free Open-Source Solution

If you want to keep it 100% free, the current implementation is the best you can do with free Avalonia controls. You could:

1. Add a tooltip explaining the selection limitation
2. Add a "Copy All" button for read mode
3. Promote the "Export to HTML" feature more prominently
4. Accept that selection is per-element only

The current app is already excellent for markdown viewing - the selection limitation is minor compared to all the features it offers.
