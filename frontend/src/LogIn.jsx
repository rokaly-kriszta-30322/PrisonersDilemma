import { useRef, useState, useEffect, useContext } from "react";
import AuthContext from "./context/AuthProvider";
import axios from "./api/axios";
import { Link, useNavigate } from 'react-router-dom';

const LOGIN_URL = '/UserData/login';

const LogIn = () => {

    const { setAuth } = useContext(AuthContext);

    const userRef = useRef();
    const errRef = useRef();
    const navigate = useNavigate();

    const [user, setUser] = useState('');
    const [pwd, setPwd] = useState('');
    const [errMsg, setErrMsg] = useState('');

    useEffect(() => {
        userRef.current.focus();
    }, [])

    useEffect(() => {
        setErrMsg('');
    }, [user, pwd])

    const handleSubmit = async (e) => {
        e.preventDefault();

        try
        {
            const response = await axios.post(LOGIN_URL, JSON.stringify({UserName: user, Password: pwd}), {
                headers: { 'Content-Type': 'application/json' },
            });

            const token = response?.data.token;
            const userData = response?.data?.user;

            setAuth({user: userData, pwd, token});
            setUser('');
            setPwd('');
            navigate('/menu', { replace: true });
        }
        catch (err)
        {
            if(!err?.response) {
                setErrMsg('No Server Response');
            }
            else if (err.response?.status === 400 ) {
                setErrMsg('Missing Username or Password');
            }
            else {
                setErrMsg('Login Failed')
            }
            errRef.current.focus();
        }

    }

    return (
        <div className="signup">
            <section>
                <p ref={errRef} className={errMsg ? "errmsg" : "offscreen"} aria-live="assertive">{errMsg}</p>
                <h1>Log In!</h1>
                <form onSubmit={handleSubmit}>
                    <label htmlFor="username">Username:</label>
                    <input
                        type="text"
                        id="username"
                        ref={userRef}
                        autoComplete="off"
                        onChange={(e) => setUser(e.target.value)}
                        value={user}
                        required
                    />
                    <label htmlFor="password">Password:</label>
                    <input
                        type="password"
                        id="password"
                        onChange={(e) => setPwd(e.target.value)}
                        value={pwd}
                        required
                    />
                    <button>Log In!</button>
                    <p>
                        Need an Account?<br />
                        <span className="line">
                            <Link to="/signup">Sign Up!</Link>
                        </span>
                    </p>
                </form>
                
            </section>
        </div>
    )
}

export default LogIn