// radio wiring mini-game, kind of like the "Flow" app. you drag a line between the two dots of
// the same color without crossing the other lines. 3 levels: 3x3, 4x4, 5x5. when the last one is
// solved it calls onComplete(). closing it just cancels.

const COLORS = {
  red: "#e53935",
  blue: "#29b6f6",
  green: "#43a047",
  yellow: "#fdd835",
  purple: "#8e24aa"
};

// the levels: grid size + the color pairs to connect. i put the dots off-axis (not on the same
// row/column) so the wire always has to bend, but each one still has a solution.
const LEVELS = [
  {
    // 3x3, 2 colors
    size: 3,
    pairs: [
      { color: "red", a: [0, 0], b: [2, 2] },
      { color: "blue", a: [2, 0], b: [1, 1] }
    ]
  },
  {
    // 4x4, 3 colors
    size: 4,
    pairs: [
      { color: "red", a: [0, 0], b: [2, 3] },
      { color: "blue", a: [3, 0], b: [1, 2] },
      { color: "green", a: [3, 1], b: [2, 2] }
    ]
  },
  {
    // 5x5, 4 colors
    size: 5,
    pairs: [
      { color: "red", a: [0, 0], b: [2, 4] },
      { color: "blue", a: [4, 0], b: [1, 2] },
      { color: "green", a: [4, 1], b: [3, 4] },
      { color: "yellow", a: [2, 1], b: [3, 2] }
    ]
  }
];

// hex -> rgba so i can make the wire fill see-through
function hexToRgba(hex, alpha) {
  const value = hex.replace("#", "");
  const r = parseInt(value.substring(0, 2), 16);
  const g = parseInt(value.substring(2, 4), 16);
  const b = parseInt(value.substring(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

// opens the puzzle and runs the levels one after another
export function openRadioWirePuzzle(onComplete) {
  const overlay = document.createElement("div");
  overlay.className = "wire-overlay";
  document.body.appendChild(overlay);

  let levelIndex = 0;

  function close() {
    overlay.remove();
  }

  function handleLevelSolved() {
    levelIndex += 1;

    if (levelIndex >= LEVELS.length) {
      close();
      if (typeof onComplete === "function") {
        onComplete();
      }
      return;
    }

    renderLevel();
  }

  function renderLevel() {
    overlay.innerHTML = "";

    const modal = document.createElement("div");
    modal.className = "wire-modal";
    overlay.appendChild(modal);

    const level = LEVELS[levelIndex];

    const title = document.createElement("h2");
    title.className = "wire-title";
    title.textContent = `Wire the radio — step ${levelIndex + 1} of ${LEVELS.length}`;
    modal.appendChild(title);

    const hint = document.createElement("p");
    hint.className = "wire-hint";
    hint.textContent = `Connect all ${level.pairs.length} colours. Drag from a dot to its match without crossing another wire.`;
    modal.appendChild(hint);

    buildLevelGrid(level, modal, handleLevelSolved);

    const actions = document.createElement("div");
    actions.className = "wire-actions";
    modal.appendChild(actions);

    const resetButton = document.createElement("button");
    resetButton.type = "button";
    resetButton.className = "btn btn-secondary";
    resetButton.textContent = "Reset";
    actions.appendChild(resetButton);

    const cancelButton = document.createElement("button");
    cancelButton.type = "button";
    cancelButton.className = "btn btn-danger";
    cancelButton.textContent = "Close";
    cancelButton.addEventListener("click", close);
    actions.appendChild(cancelButton);

    // reset just builds the same level again
    resetButton.addEventListener("click", renderLevel);
  }

  renderLevel();
}

// builds the grid for one level and sets up the drag-to-connect
function buildLevelGrid(level, container, onSolved) {
  const size = level.size;
  const cellCount = size * size;
  const owner = new Array(cellCount).fill(null); // which color is in each cell (null = empty)
  const endpoints = {};                          // cell index -> color (the fixed dots)
  const paths = {};                              // color -> list of cells in its wire

  level.pairs.forEach(pair => {
    const indexA = pair.a[0] * size + pair.a[1];
    const indexB = pair.b[0] * size + pair.b[1];
    endpoints[indexA] = pair.color;
    endpoints[indexB] = pair.color;
    owner[indexA] = pair.color;
    owner[indexB] = pair.color;
    paths[pair.color] = [];
  });

  const grid = document.createElement("div");
  grid.className = "wire-grid";
  grid.style.gridTemplateColumns = `repeat(${size}, 1fr)`;
  grid.style.gridTemplateRows = `repeat(${size}, 1fr)`;

  const cells = [];
  for (let i = 0; i < cellCount; i += 1) {
    const cell = document.createElement("div");
    cell.className = "wire-cell";
    cell.dataset.index = String(i);

    if (endpoints[i]) {
      const dot = document.createElement("span");
      dot.className = "wire-dot";
      dot.style.background = COLORS[endpoints[i]];
      cell.appendChild(dot);
    }

    grid.appendChild(cell);
    cells.push(cell);
  }

  container.appendChild(grid);

  let drawing = null;

  function endpointsOf(color) {
    return Object.keys(endpoints)
      .filter(index => endpoints[index] === color)
      .map(Number);
  }

  function isConnected(color) {
    const ends = endpointsOf(color);
    return ends.every(end => paths[color].includes(end));
  }

  function isSolved() {
    return Object.keys(paths).every(isConnected);
  }

  function clearColor(color) {
    paths[color].forEach(index => {
      if (!endpoints[index]) {
        owner[index] = null;
      }
    });
    paths[color] = [];
  }

  function truncatePathTo(color, index) {
    const position = paths[color].indexOf(index);
    if (position < 0) {
      return;
    }

    for (let k = position + 1; k < paths[color].length; k += 1) {
      const freed = paths[color][k];
      if (!endpoints[freed]) {
        owner[freed] = null;
      }
    }

    paths[color] = paths[color].slice(0, position + 1);
  }

  function indexFromPoint(x, y) {
    const element = document.elementFromPoint(x, y);
    if (!element) {
      return -1;
    }

    const cell = element.closest(".wire-cell");
    if (!cell || !grid.contains(cell)) {
      return -1;
    }

    return Number(cell.dataset.index);
  }

  function isAdjacent(a, b) {
    const ra = Math.floor(a / size);
    const ca = a % size;
    const rb = Math.floor(b / size);
    const cb = b % size;
    return Math.abs(ra - rb) + Math.abs(ca - cb) === 1;
  }

  function startAt(index) {
    if (endpoints[index]) {
      const color = endpoints[index];
      clearColor(color);
      paths[color] = [index];
      drawing = color;
      redraw();
      return;
    }

    const color = owner[index];
    if (color && paths[color].includes(index)) {
      truncatePathTo(color, index);
      drawing = color;
      redraw();
    }
  }

  function extendTo(index) {
    if (!drawing) {
      return;
    }

    const path = paths[drawing];
    const last = path[path.length - 1];

    if (index === last || !isAdjacent(index, last)) {
      return;
    }

    // already hit the end dot, so don't keep going
    if (endpoints[last] && last !== path[0]) {
      return;
    }

    if (path.includes(index)) {
      truncatePathTo(drawing, index);
      redraw();
      return;
    }

    const occupant = owner[index];

    if (occupant === null) {
      owner[index] = drawing;
      path.push(index);
      redraw();
      return;
    }

    // reached this color's other dot -> it's connected
    if (occupant === drawing && endpoints[index]) {
      path.push(index);
      redraw();
    }
    // if the cell belongs to another color it's blocked, so you go around it
  }

  function redraw() {
    cells.forEach(cell => {
      cell.style.background = "";
      cell.classList.remove("wire-cell-on");
    });

    Object.keys(paths).forEach(color => {
      paths[color].forEach(index => {
        cells[index].style.background = hexToRgba(COLORS[color], 0.5);
        cells[index].classList.add("wire-cell-on");
      });
    });
  }

  grid.addEventListener("pointerdown", event => {
    event.preventDefault();
    const index = indexFromPoint(event.clientX, event.clientY);
    if (index >= 0) {
      try {
        grid.setPointerCapture(event.pointerId);
      } catch (error) {
        // pointer capture is just a nice-to-have, still works without it
      }
      startAt(index);
    }
  });

  grid.addEventListener("pointermove", event => {
    if (!drawing) {
      return;
    }
    const index = indexFromPoint(event.clientX, event.clientY);
    if (index >= 0) {
      extendTo(index);
    }
  });

  grid.addEventListener("pointerup", () => {
    drawing = null;
    if (isSolved()) {
      onSolved();
    }
  });

  grid.addEventListener("pointercancel", () => {
    drawing = null;
  });
}
