# Design System - Motherson Box Management

This design system is built for the high-stakes environment of industrial packaging areas (specifically the P3 packaging workstation). The brand personality focuses on reliability, absolute clarity, and utility, avoiding decorative trends, shadows, or visual noise.

## Core Visual Attributes
* **Style**: Industrial Corporate Modern
* **Principles**: High contrast, keyboard/scanner friendly, calm surface backgrounds, clear visual hierarchy.
* **Layout**: Desktop-first layout with a persistent left sidebar on desktop to save vertical space and keep workstation terminals clean.

---

## 1. Color Palette (Design Tokens)

### Brand & Surface Colors
* **Primary Brand Red (`#E51E25`)**: The official brand red. Used for primary call-to-actions, brand accents, and critical warnings. Never overused.
* **Background (`#F8F9FB`)**: A light, low-glare off-white baseline that reduces eye fatigue for operators standing at terminals.
* **Surface Container Lowest (`#FFFFFF`)**: Pure white. Reserved for cards, tables, and modal backgrounds.
* **Surface Container (`#EDEEF0`)**: Light grey. Used for input fields, backgrounds for disabled states, or sidebar background.
* **Text Main (`#1F2937`)**: Dark grey. Provides optimal text contrast.
* **Text High Emphasis (`#111111`)**: Deep black for headings and prominent labels.
* **Borders & Outlines (`#E5E7EB`)**: A clean line separator. Avoids fuzzy shadows.

### Semantic Status Badges & Chips
Status indicators must use desaturated backgrounds with high-contrast text to communicate state clearly, complemented by text labels and distinct icons where possible:

| Status Name | Text Color | Background Color | Visual Meaning |
| :--- | :--- | :--- | :--- |
| **Open** | `#16A34A` (Green) | `#DCFCE7` (Light Green) | Box is currently open for scans |
| **Completed** | `#E51E25` (Red) | `#FDE8E8` (Light Red) | Expected quantity met (Auto-closed) |
| **Completed with Exception** | `#0284C7` (Blue) | `#E0F2FE` (Light Blue) | Force-closed by a supervisor with deviation |
| **Cancelled** | `#DC2626` (Dark Red) | `#FEE2E2` (Light Red) | Cancelled by a supervisor |
| **Blocked** | `#D97706` (Orange) | `#FEF3C7` (Light Amber) | Temporarily quarantined / blocked |
| **Archived** | `#4B5563` (Gray) | `#F3F4F6` (Light Gray) | Read-only box moved to long-term storage |

---

## 2. Typography

We use **Inter** or standard system sans-serif fallback stack for maximum legibility of numbers and barcodes.

* **Display Large**: `36px` / SemiBold (e.g. key metrics, big totals)
* **Headline Large**: `28px` / SemiBold (e.g. page headers)
* **Headline Medium**: `20px` / SemiBold (e.g. card titles, modal titles)
* **Body Large**: `16px` / Regular (e.g. form fields, primary text)
* **Body Medium**: `14px` / Regular (e.g. tables, secondary text)
* **Label Large**: `14px` / Bold (e.g. form labels, table headers)
* **Label Medium**: `12px` / Medium (e.g. metadata tags, timestamps)

---

## 3. Shapes & Layout System

* **Cards**: Pure white background, `1px` solid border (`#E5E7EB`), `12px` border-radius (`rounded-lg`). No soft floating shadows.
* **Inputs & Form Controls**: `1px` solid border (`#E5E7EB`), `8px` corner radius. Focused inputs use a `2px` brand red outline for clear scanning indicator.
* **Interactive Elements Height**:
  * **Primary actions**: `48px` minimum height (easy to tap/click while standing).
  * **Secondary/Table actions**: `44px` minimum height.
* **Spacing Grid**: A strict `4px` baseline spacing system (`8px`, `12px`, `16px`, `24px`, `32px`).
* **Table Rows**: `56px` minimum row height with zebra striping (Off-white / White) for tables with many rows. Horizontal outlines only.
