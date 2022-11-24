import Logo from "./assets/Logo.png";

import styles from "./CustomerDashboardHeader.module.css";
import { useEffect, useState, useRef } from "react";
import { useNavigate } from "react-router-dom";
import axios from "axios";
import CogWheel from "./assets/cogwheel.png";
import AddZendeskTicket from "../popupWindows/AddZendeskTicket";
import AddJiraRequest from "../popupWindows/AddJiraRequest";

export default function CustomerDashboardHeader() {
  useEffect(() => {
    getInfo();
  }, []);

  const[addZenTicketPopup, setAddZenTicketPopup] = useState(false);
  const[addJiraRequest, setAddJiraRequest] = useState(false);

  
  const navigate = useNavigate();
  const [active, setActive] = useState(0);
  const [closed, setClosed] = useState(0);
  const [open, setOpen] = useState(false);
  
  let menuRef = useRef();
  useEffect(()=>{
    let handler = (e)=>{
      if(!menuRef.current.contains(e.target)){
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", handler);
  });

  async function getInfo() {
    try {
      const response = await axios.get(
        "https://localhost:7001/api/DashboardInfo"
      );
      setActive(response.data.activeTickets);
      setClosed(response.data.closedTickets);
    } catch (error) {}
    }
    function logout() {
      localStorage.removeItem("token");
      localStorage.removeItem('userType');
      navigate("/");
    }

  function OpenPopupWindow(){
    
  }

  return (
    <div id={styles.headerContainer}>
      <AddZendeskTicket trigger={addZenTicketPopup} setTrigger={setAddZenTicketPopup}></AddZendeskTicket>
      <AddJiraRequest trigger={addJiraRequest} setTrigger={setAddJiraRequest}></AddJiraRequest>
      <div id={styles.logoContainer}>
        <img src={Logo} id={styles.logo} alt="NativeSoftware Logo" />
      </div>
      <div className={styles.infoContainer}>
        <div className={styles.infoTitle}>Active Support Tickets</div>
        <div className={styles.infoValue}>{active}</div>
      </div>
      <div className={styles.infoContainer}>
        <div className={styles.infoTitle}>Closed Tickets This Week</div>
        <div className={styles.infoValue}>{closed}</div>
      </div>
      <div id={styles.settings}>
        <p id={styles.userName}>TechSolutions</p>
        <div id={styles.logout} onClick={logout}>Logout</div>
        
        <div id={styles.menuContainer} ref={menuRef}>
          <div id={styles.menuTrigger} onClick={()=>{setOpen(!open)}}>
            <img src={CogWheel} id={styles.cogwheelDropdown} alt="CogWheel" />
          </div>
          {open?
            <div id={styles.dropdownMenu} onClick={() => { setOpen(!open) }}>
              <ul>
              <li className={styles.dropdownItem} onClick={()=>setAddJiraRequest(true)}><h3>New Jira Request</h3></li>
              <li className={styles.dropdownItem} onClick={()=>setAddZenTicketPopup(true)}><h3>Add Zendesk Ticket</h3></li>
              <li className={styles.dropdownItem} onClick={()=>setAddZenTicketPopup(true)}><h3>View Response Time</h3></li>
              </ul>
            </div>
            :
            null
          }
        </div>
      </div>
    </div>
  );
}