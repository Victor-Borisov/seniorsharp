// Regenerate the standalone skill-graph explorer by re-injecting the canonical
// skill-graph data into the page's inlined `const GRAPH = ...;` declaration.
//
// The explorer (frontend/public/skill-graph.html) is a single self-contained file:
// the visualization code plus a snapshot of the graph data baked in, so it opens
// directly in a browser with no server or fetch. That snapshot goes stale whenever
// content/skill-graph.json changes - run this script to refresh it.
//
// Edit the page's UI/markup in frontend/public/skill-graph.html (the canonical copy).
// This script rewrites only the single data line in it, then mirrors the whole file
// to ../docs/skill-graph.html (the local demo copy) if that directory exists.
//
//   npm run regen-graph        (from frontend/)
//   node scripts/regen-skill-graph.mjs
//
// Exit code is non-zero on any failure so it is safe to chain in build steps.

import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDir = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(scriptDir, '..', '..');           // .../src
const jsonPath = resolve(repoRoot, 'content', 'skill-graph.json');
const canonicalHtml = resolve(repoRoot, 'frontend', 'public', 'skill-graph.html');
const mirrorHtml = resolve(repoRoot, '..', 'docs', 'skill-graph.html'); // outside the repo

const MARKER = 'const GRAPH = ';

function fail(msg) {
  console.error(`regen-skill-graph: ${msg}`);
  process.exit(1);
}

// 1. Load and validate the canonical data.
let graph;
try {
  graph = JSON.parse(readFileSync(jsonPath, 'utf8'));
} catch (e) {
  fail(`cannot read/parse ${jsonPath}: ${e.message}`);
}
const nodeCount = Array.isArray(graph.nodes) ? graph.nodes.length : 0;
if (!nodeCount) fail(`no nodes found in ${jsonPath}`);
const edgeCount = graph.nodes.reduce((n, x) => n + (x.prerequisites?.length ?? 0), 0);

// Compact single-line payload; escape "</" so it cannot terminate the <script> block.
const payload = JSON.stringify(graph).replaceAll('</', '<\\/');

// 2. Re-inject into the canonical page. The declaration occupies exactly one physical
//    line, so we replace from the marker to that line's end (NOT to the next ";", which
//    can legitimately appear inside JSON string values).
function reinject(html, where) {
  const start = html.indexOf(MARKER);
  if (start === -1) fail(`marker "${MARKER}" not found in ${where}`);
  if (html.indexOf(MARKER, start + MARKER.length) !== -1)
    fail(`marker "${MARKER}" appears more than once in ${where}`);
  const lineEnd = html.indexOf('\n', start);
  if (lineEnd === -1) fail(`no end-of-line after marker in ${where}`);
  return html.slice(0, start) + MARKER + payload + ';' + html.slice(lineEnd);
}

const updatedHtml = reinject(readFileSync(canonicalHtml, 'utf8'), canonicalHtml);
writeFileSync(canonicalHtml, updatedHtml);

// 3. Mirror the regenerated page to the local demo copy, if present.
let mirrored = false;
if (existsSync(mirrorHtml)) {
  writeFileSync(mirrorHtml, updatedHtml);
  mirrored = true;
}

console.log(
  `regen-skill-graph: injected ${nodeCount} nodes / ${edgeCount} edges ` +
  `(graph ${graph.version ?? 'unknown'}) into:\n` +
  `  - ${canonicalHtml}` +
  (mirrored ? `\n  - ${mirrorHtml}` : `\n  (mirror ${mirrorHtml} not present - skipped)`)
);
