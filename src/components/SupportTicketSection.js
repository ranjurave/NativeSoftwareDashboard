import React from 'react'
import axios from 'axios';
import { useEffect, useState, CSSProperties } from 'react'
import styles from "./SupportTicket.module.css"
import SupportTicket from './SupportTicket'
import FadeLoader  from "react-spinners/FadeLoader";

function SupportTicketSection() {
  const [loading, setLoading] = useState(true);
  const [ticketList, setTicketList] = useState([]);

  useEffect(() =>{
    setLoading(false);
    setTimeout(() => {
      setLoading(true)
    })
  },[])
  
  //Zendesk API
  useEffect(() => {
      getTicket();
  },[]);

  async function getTicket() {
    try {
      const response = await axios.get('https://localhost:7001/api/Zendesk')
      setTicketList(response.data);
      setLoading(!loading);
    }
    catch (error) {
      console.error(error);
    }
  }

  const TicketElements = ticketList.map((element) =>{
    return <SupportTicket key={element.id} ticket={element}/>;
  });
  
  return (
    <div id={styles.supportticketsection}>
      <div id={styles.ticketheadings}>
          <p id={styles.org}> Organisation</p>
          <p id={styles.sub}> Subject</p>
          <p id={styles.status}> Status</p>
          <p id={styles.assign}> Assigned</p>
          <p id={styles.bill}> Billable</p>
          <p id={styles.priority}> Priority</p>
          <p id={styles.reqtime}> Requested Time</p>
          <p id={styles.due}> Time Due</p>
          <p id={styles.type}> Type</p>
          <div id = {styles.links}></div>
      </div>

      { 
      loading?
      <FadeLoader
        color="#81E8FF"
        height={17}
        margin={6}
        width={4}
      /> :
      TicketElements
      } 

      <div>
        <div id={styles.chevronArrowDown}></div>
      </div>
    </div>
  )
}

export default SupportTicketSection