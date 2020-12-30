import React from 'react';
import styled, { keyframes } from 'styled-components';
import TeleportImg from '../../assets/img/siege/userAction/effect_user_teleport.png'
import './Siege.css';

const Teleport = props => {
    return (
        <img className="TeleportEnterPlayer" src={TeleportImg} />
    );
}

export default Teleport;