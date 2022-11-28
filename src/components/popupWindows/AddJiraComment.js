import axios from 'axios';
import { useEffect, useState, useRef } from "react";
import styles from "./PopupWindows.module.css";


function AddJiraComment(props) {
    return (props.trigger)?(
        <div id={styles.popup} >
            <div id={styles.popupinner}>
                <div id={styles.userName}>
                    TechSolutions
                </div>
                <div  id={styles.windowName}>
                    Add New Jira Comment                
                </div>
            <button id={styles.closeButton} onClick={()=> props.setTrigger(false)}>Cancel</button>
            </div>
        </div>):"";
}

export default AddJiraComment

