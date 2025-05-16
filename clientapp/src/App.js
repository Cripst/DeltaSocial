import React, { useEffect, useState } from 'react';
import logo from './logo.svg';
import './App.css';
import Auth from './Auth';

function App() {
    const [message, setMessage] = useState('Încarcare...');

    useEffect(() => {
        fetch('https://localhost:7236/api/test')
            .then(response => response.json())
            .then(data => setMessage(data.message))
            .catch(() => setMessage('Eroare la conexiunea cu backend-ul'));
    }, []);

    return (
        <div className="App">
            <header className="App-header">
                <img src={logo} className="App-logo" alt="logo" />
                <p>{message}</p>
                <Auth />
            </header>
        </div>
    );
}

export default App;
