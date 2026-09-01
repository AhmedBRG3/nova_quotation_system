#!/usr/bin/env node
/* Copies the static site into ./build — that folder is what gets uploaded
   to GoDaddy (drop its *contents* into public_html). */

const fs = require('fs');
const path = require('path');

const root = __dirname;
const out = path.join(root, 'build');
const items = ['index.html', 'css', 'js', 'assets', '.htaccess'];

fs.rmSync(out, { recursive: true, force: true });
fs.mkdirSync(out, { recursive: true });

let files = 0;
let bytes = 0;

function copy(src, dest) {
  const stat = fs.statSync(src);
  if (stat.isDirectory()) {
    fs.mkdirSync(dest, { recursive: true });
    for (const entry of fs.readdirSync(src)) {
      if (entry === '.DS_Store') continue;
      copy(path.join(src, entry), path.join(dest, entry));
    }
  } else {
    fs.copyFileSync(src, dest);
    files += 1;
    bytes += stat.size;
  }
}

for (const item of items) {
  const src = path.join(root, item);
  if (!fs.existsSync(src)) continue;
  copy(src, path.join(out, item));
}

console.log(`build ready → ${out}`);
console.log(`${files} files, ${(bytes / 1024).toFixed(1)} KB`);
console.log('upload the CONTENTS of build/ into public_html on GoDaddy');
