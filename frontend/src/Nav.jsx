import { useContext } from 'react';
import { useNavigate } from 'react-router-dom';
import AuthContext from './context/AuthProvider';
import axios from './api/axios';

const Nav = () => {
    const { auth, setAuth } = useContext(AuthContext);
    const navigate = useNavigate();

    const handleLogout = async () => {
        try {
        await axios.post('/UserData/logout', {}, {
            headers: {
            Authorization: `Bearer ${auth.token}`
            }
        });
        } catch (err) {
        console.warn("Logout backend call failed.");
        }
        setAuth({});
        localStorage.removeItem("auth");
        navigate('/login');
    };

    return (
        <nav>
            <div className="left">
                {auth.user && <span>Welcome, {auth.user.userName}</span>}
            </div>
            <div className="right">
                <button onClick={handleLogout}>Logout</button>
            </div>
        </nav>
    );
};

export default Nav;