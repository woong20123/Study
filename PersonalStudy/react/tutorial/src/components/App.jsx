import logo from '../logo.svg';
import Death from './Siege/Death.jsx'
import Teleport from './Siege/Teleport.jsx'
import './App.css';

function App() {
  return (
    <div className="App">
      <header className="App-header">
        <Death/>
        <Teleport/>
      </header>
    </div>
  );
}

export default App;
