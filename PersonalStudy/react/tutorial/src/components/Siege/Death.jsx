import React from 'react';
import styled, { keyframes } from 'styled-components';
import DeathImg from '../../assets/img/siege/userAction/effect_user_die.png'
import './Siege.css';

// const boxFade = keyframes`
//     from{
//         top:50px;
//     }
//     to{
//         top:100px;
//     }
// `

// const StyledWrapper = styled.div`
//     animation: ${boxFade} 1s 1s linear alternate;
// `;

const Death = props => {
    return (
        <img className="DeathPlayer" src={DeathImg} />
    );
}

export default Death