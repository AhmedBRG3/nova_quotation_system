(function () {
  'use strict';

  var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  /* current year in the footer */
  var year = document.getElementById('year');
  if (year) year.textContent = String(new Date().getFullYear());

  /* split the headline into words so each can animate in on its own */
  var headline = document.querySelector('[data-split]');
  if (headline) {
    var words = headline.textContent.trim().split(/\s+/);
    headline.textContent = '';
    words.forEach(function (word, i) {
      var span = document.createElement('span');
      span.className = 'word';
      span.style.setProperty('--i', i);
      span.textContent = word;
      headline.appendChild(span);
      if (i < words.length - 1) headline.appendChild(document.createTextNode(' '));
    });
  }

  /* progress read-out — eases toward 99% and holds, because the site is
     genuinely still being built rather than finishing on a timer */
  var bar = document.querySelector('.progress');
  var fill = document.querySelector('.progress__fill');
  var value = document.querySelector('.progress__value');
  var phase = document.querySelector('.progress__phase');
  if (!bar || !fill || !value || !phase) return;

  var phases = [
    { at: 0,  label: 'Initializing' },
    { at: 22, label: 'Loading brand assets' },
    { at: 45, label: 'Building layouts' },
    { at: 68, label: 'Optimizing experience' },
    { at: 88, label: 'Almost there' }
  ];

  var current = 0;
  var target = 99;

  function paint(pct) {
    var rounded = Math.round(pct);
    fill.style.width = pct.toFixed(1) + '%';
    value.textContent = rounded + '%';
    bar.setAttribute('aria-valuenow', String(rounded));

    var label = phases[0].label;
    for (var i = 0; i < phases.length; i++) {
      if (rounded >= phases[i].at) label = phases[i].label;
    }
    if (phase.textContent !== label) phase.textContent = label;
  }

  if (reduceMotion) {
    paint(target);
    return;
  }

  function tick() {
    /* ease out: big steps early, crawling steps near the end */
    current += (target - current) * 0.035 + 0.08;
    if (current > target) current = target;
    paint(current);
    if (target - current > 0.15) requestAnimationFrame(tick);
  }

  setTimeout(function () { requestAnimationFrame(tick); }, 700);
})();
