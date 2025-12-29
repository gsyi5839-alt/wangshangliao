<template>
  <div id="app-root">
    <RouterView />
  </div>
</template>

<script setup lang="ts">
/**
 * Root Application Component
 * Provides the main app container with global styling
 */
import { RouterView } from 'vue-router'
</script>

<style>
/* Global App Styles */
#app-root {
  min-height: 100vh;
  background: var(--bg-darkest);
  color: var(--text-primary);
}

/* Selection Styling */
::selection {
  background: rgba(32, 165, 58, 0.3);
  color: var(--text-primary);
}

/* Focus Visible Styling */
:focus-visible {
  outline: 2px solid var(--primary);
  outline-offset: 2px;
}

/* Smooth Scrolling */
html {
  scroll-behavior: smooth;
}

/* Link Reset */
a {
  color: inherit;
  text-decoration: none;
}

/* Button Reset */
button {
  font-family: inherit;
}
</style>
