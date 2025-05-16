import React, { useState } from 'react';

function Auth() {
    const [isRegister, setIsRegister] = useState(false);
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [message, setMessage] = useState('');

    const handleSubmit = async (e) => {
        e.preventDefault();
        const url = isRegister ? 'https://localhost:7236/api/auth/register' : 'https://localhost:7236/api/auth/login';
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        if (response.ok) {
            const data = await response.text();
            setMessage(data);
        } else {
            setMessage('Eroare la autentificare.');
        }
    };

    return (
        <div style={{ maxWidth: 400, margin: 'auto', padding: 20 }}>
            <h2>{isRegister ? 'Înregistrare' : 'Login'}</h2>
            <form onSubmit={handleSubmit}>
                <input
                    type="email"
                    placeholder="Email"
                    value={email}
                    onChange={e => setEmail(e.target.value)}
                    required
                    style={{ width: '100%', padding: 8, marginBottom: 10 }}
                />
                <input
                    type="password"
                    placeholder="Parolă"
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                    required
                    style={{ width: '100%', padding: 8, marginBottom: 10 }}
                />
                <button type="submit" style={{ padding: 10, width: '100%' }}>
                    {isRegister ? 'Înregistrează-te' : 'Autentifică-te'}
                </button>
            </form>
            <p style={{ marginTop: 10 }}>
                {isRegister ? 'Ai deja cont?' : 'Nu ai cont?'}{' '}
                <button
                    onClick={() => setIsRegister(!isRegister)}
                    style={{ color: 'blue', background: 'none', border: 'none', cursor: 'pointer' }}
                >
                    {isRegister ? 'Login' : 'Înregistrare'}
                </button>
            </p>
            {message && <p>{message}</p>}
        </div>
    );
}

export default Auth;
