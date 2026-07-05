---
name: Motherson Box Management
colors:
  surface: '#f8f9fb'
  surface-dim: '#d9dadc'
  surface-bright: '#f8f9fb'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f2f4f6'
  surface-container: '#edeef0'
  surface-container-high: '#e7e8ea'
  surface-container-highest: '#e1e2e4'
  on-surface: '#191c1e'
  on-surface-variant: '#5d3f3c'
  inverse-surface: '#2e3132'
  inverse-on-surface: '#f0f1f3'
  outline: '#926e6b'
  outline-variant: '#e7bdb8'
  surface-tint: '#c00015'
  primary: '#bc0015'
  on-primary: '#ffffff'
  primary-container: '#e51e25'
  on-primary-container: '#fffcff'
  inverse-primary: '#ffb4ac'
  secondary: '#5f5e5e'
  on-secondary: '#ffffff'
  secondary-container: '#e5e2e1'
  on-secondary-container: '#656464'
  tertiary: '#535d6d'
  on-tertiary: '#ffffff'
  tertiary-container: '#6c7686'
  on-tertiary-container: '#fffdff'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#ffdad6'
  primary-fixed-dim: '#ffb4ac'
  on-primary-fixed: '#410002'
  on-primary-fixed-variant: '#93000d'
  secondary-fixed: '#e5e2e1'
  secondary-fixed-dim: '#c8c6c5'
  on-secondary-fixed: '#1c1b1b'
  on-secondary-fixed-variant: '#474646'
  tertiary-fixed: '#d9e3f6'
  tertiary-fixed-dim: '#bdc7d9'
  on-tertiary-fixed: '#121c2a'
  on-tertiary-fixed-variant: '#3d4756'
  background: '#f8f9fb'
  on-background: '#191c1e'
  surface-variant: '#e1e2e4'
typography:
  display-lg:
    fontFamily: Inter
    fontSize: 36px
    fontWeight: '700'
    lineHeight: 44px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Inter
    fontSize: 28px
    fontWeight: '600'
    lineHeight: 36px
    letterSpacing: -0.01em
  headline-md:
    fontFamily: Inter
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  body-lg:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  label-lg:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
  label-md:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
  headline-lg-mobile:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  base: 4px
  xs: 8px
  sm: 12px
  md: 16px
  lg: 24px
  xl: 32px
  gutter: 24px
  margin: 32px
  container-max: 1440px
---

## Brand & Style

This design system is built for the high-stakes environment of B2B industrial management. The brand personality is rooted in reliability, precision, and sobriety. It eschews decorative trends in favor of functional clarity, ensuring that users can manage box inventory and logistics without visual fatigue or ambiguity.

The design style is **Corporate Modern** with a focus on **High-Contrast** utility. It utilizes a flat, structural aesthetic that mimics the efficiency of a well-organized physical workspace. The interface relies on substantial white space, rigid grid alignment, and a strict adherence to a limited color palette to evoke a sense of security and professional rigor.

## Colors

The palette is intentionally restrained to maintain an industrial, sober atmosphere. 

- **Primary Red (#E51E25):** Used sparingly for primary actions, critical alerts, and brand accents. Its high visibility ensures key touchpoints are never missed.
- **Surface & Backgrounds:** The main application background uses **Off-white (#F7F8FA)** to reduce glare, while component surfaces use pure White (#FFFFFF) to create clear content separation.
- **Typography:** **Text (#1F2937)** provides high legibility against white backgrounds, while **Black (#111111)** is reserved for headings and high-emphasis labels.
- **Borders:** **Light Gray (#E5E7EB)** defines the boundaries of the workspace, providing structure without adding visual noise.

## Typography

Inter is chosen for its exceptional readability in data-heavy environments. The scale follows a strict hierarchy to help users scan inventory lists and dashboard metrics quickly. 

Use `display-lg` for dashboard summaries and `headline-md` for card titles. For tabular data, `body-md` is the standard to allow for high information density without sacrificing clarity. Labels should use the `uppercase` variant to distinguish metadata from content.

## Layout & Spacing

The design system utilizes a **Fixed Grid** model for desktop to ensure data visualization remains consistent across professional monitors.

- **Desktop (1440px+):** 12-column grid, 24px gutters, 32px side margins.
- **Tablet (768px - 1439px):** 8-column grid, 16px gutters, 24px margins.
- **Mobile (<767px):** 4-column grid, 12px gutters, 16px margins.

Spacing follows a 4px baseline. Components like input fields and buttons utilize "Large Clickable Areas" (minimum 44px height) to accommodate use in industrial settings where precision interaction is required.

## Elevation & Depth

To maintain a "sober" and "industrial" feel, this design system avoids soft ambient shadows. Instead, it uses **Tonal Layers** and **Low-contrast outlines**.

1.  **Level 0 (Background):** Off-white (#F7F8FA) base.
2.  **Level 1 (Cards/Content):** Pure White (#FFFFFF) surface with a 1px solid border (#E5E7EB). No shadow.
3.  **Level 2 (Active/Modals):** Pure White (#FFFFFF) with a thin 1px border (#111111) or a very tight, 4px blur shadow with 5% opacity to indicate temporary overlay.

Depth is communicated through structure and containment rather than lighting effects.

## Shapes

The shape language reflects the "Box Management" theme—structured and sturdy.

- **Cards & Primary Containers:** Use 12px corner radius (`rounded-lg` in this system) to soften the industrial edge while maintaining a professional look.
- **Inputs & Small Buttons:** Use 8px corner radius for a precise, modern feel.
- **Status Badges:** Use 4px corner radius; avoid pill shapes to keep the aesthetic "sober" rather than playful.

## Components

### Buttons
- **Primary:** Solid Red (#E51E25) with white text. 48px minimum height for high-traffic industrial use.
- **Secondary:** Solid Black (#111111) or transparent with a 2px black border.
- **States:** Hover states should simply darken the background color by 10%. No gradients or glows.

### Cards
- **Construction:** White background, 1px border (#E5E7EB), 12px border-radius.
- **Header:** Cards should include a 16px padding top/bottom header section with a subtle bottom divider if they contain complex data.

### Input Fields
- **Style:** 1px border (#E5E7EB), 8px radius. Use a 2px Red (#E51E25) border for the focus state. Labels must always be visible above the field (no floating labels) for maximum accessibility.

### Lists & Tables
- **Rows:** 56px minimum height. Use subtle zebra striping (Off-white) for tables exceeding 10 rows.
- **Borders:** Only horizontal dividers (#E5E7EB) to maintain a clean, scanned vertical flow.

### Status Chips
- **Geometry:** Rectangular with 4px radius.
- **Coloring:** Use desaturated background tints with high-contrast text for status (e.g., Light Red background with Dark Red text for "Delayed").