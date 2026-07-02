let choices = ['nothing', 'ian', 'something big', 'hot sauce', 'lego'];
const colors = ['blue', 'purple', 'magenta', 'crimson', 'gold', 'silver', 'coral', 'violet', 'tomato'];

let colorAssignments = {};
choices.forEach(choice => {
    if (choice === 'nothing') {
        colorAssignments[choice] = 'black';
    } else {
        let randomIndex = Math.floor(Math.random() * colors.length);
        colorAssignments[choice] = colors[randomIndex];
        console.log(choice, colors[randomIndex]);
        colors.splice(randomIndex, 1);
    }
});

let sectors = [];
const repetitions = 3;

for (let i = 0; i < repetitions; i++) { // Repeat the whole sequence
    choices.forEach(choice => {
        sectors.push({ color: colorAssignments[choice], label: choice });
    });
}

// const sectors = [
//     { color: '#f82', label: 'Stack' },
//     { color: '#0bf', label: '10' },
//     { color: '#fb0', label: '200' },
//     { color: '#0fb', label: '50' },
//     { color: '#b0f', label: '100' },
//     { color: '#f0b', label: '5' },
//     { color: '#bf0', label: '500' }
//   ]
  
  const rand = (m, M) => Math.random() * (M - m) + m
  const tot = sectors.length
  const spinEl = document.querySelector('#spin')
  const ctx = document.querySelector('#wheel').getContext('2d')
  const dia = ctx.canvas.width
  const rad = dia / 2
  const PI = Math.PI
  const TAU = 2 * PI
  const arc = TAU / sectors.length
  
  const friction = 0.991 // 0.995=soft, 0.99=mid, 0.98=hard
  let angVel = 0 // Angular velocity
  let ang = 0 // Angle in radians
  
  const getIndex = () => Math.floor(tot - (ang / TAU) * tot) % tot
  
  function drawSector(sector, i) {
    const ang = arc * i
    ctx.save()
    // COLOR
    ctx.beginPath()
    ctx.fillStyle = sector.color
    ctx.moveTo(rad, rad)
    ctx.arc(rad, rad, rad, ang, ang + arc)
    ctx.lineTo(rad, rad)
    ctx.fill()
    // TEXT
    ctx.translate(rad, rad)
    ctx.rotate(ang + arc / 2)
    ctx.textAlign = 'right'
    ctx.fillStyle = '#fff'
    ctx.font = 'bold 30px sans-serif'
    ctx.fillText(sector.label, rad - 10, 10)
    //
    ctx.restore()
  }
  
  function rotate() {
    const sector = sectors[getIndex()]
    ctx.canvas.style.transform = `rotate(${ang - PI / 2}rad)`
    spinEl.textContent = sector.label; //!angVel ? 'SPIN' : sector.label
    spinEl.style.background = sector.color
  }
  
  function frame() {
    if (!angVel) return
    angVel *= friction // Decrement velocity by friction
    if (angVel < 0.002) angVel = 0 // Bring to stop
    ang += angVel // Update angle
    ang %= TAU // Normalize angle
    rotate()
  }
  
  function engine() {
    frame()
    requestAnimationFrame(engine)
  }
  
  function init() {
    sectors.forEach(drawSector)
    rotate() // Initial rotation
    engine() // Start engine
    spinEl.addEventListener('click', () => {
      if (!angVel) angVel = rand(0.25, 0.45)
    })
  }
  
  init()
  