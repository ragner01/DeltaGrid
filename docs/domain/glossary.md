# Domain Glossary (Authoritative)

Notes

- ISO units by default. Examples: volume (m³), mass (kg), pressure (Pa), temperature (K/°C). Non-ISO usage must include conversion notes (e.g., bbl → m³, psi → Pa).
- Entries include synonyms and "Not this" clarifications to avoid ambiguity.

## Terms

1. Allocation — The process of distributing measured production to wells/units based on tests and factors. Synonyms: production allocation, balancing. Not this: financial cost allocation.
2. Allocation Factor — Weight applied to distribute commingled volumes. Synonyms: share factor. Not this: tax rate.
3. Allocation Closed — End-of-period lock of allocation results. Synonyms: allocation finalization. Not this: accounting close.
4. Allowable — Regulatory or engineering production limit. Synonyms: proration. Not this: permit allowance budget.
5. Anomaly — Deviation from expected signal patterns. Synonyms: exception. Not this: alarm (which has thresholds and semantics).
6. API Gravity — Oil density measure (°API); store SI density (kg/m³) + conversion. Synonyms: API°. Not this: application programming interface.
7. Assay — Laboratory analysis of composition/quality. Synonyms: lab result. Not this: fiscal audit.
8. Asset — Physical production entity (well, facility, pipeline). Synonyms: equipment. Not this: financial asset.
9. Audit Log — Immutable record of actions with actor, time, tenant. Synonyms: audit trail. Not this: debug log.
10. Backpressure — Controlled slowing under load to prevent overload. Synonyms: flow control. Not this: gas line pressure.
11. Barrel (bbl) — Non-ISO unit of volume; system stores m³. Conversion: 1 bbl ≈ 0.158987 m³.
12. Battery Limit — Boundary between facilities/units for measurement. Not this: electrical battery.
13. Batch — Grouped processing unit in time. Not this: chemical batch in refining.
14. Blowdown — Controlled depressurization. Not this: financial write-down.
15. Bulk Tank — Storage tank prior to custody transfer. Synonyms: stock tank. Not this: pressurized sphere.
16. Calibration — Adjustment to ensure measurement accuracy. Not this: model training.
17. Capex — Capital expenditure. Not this: opex.
18. Casing Pressure — Annulus pressure around casing (Pa). Not this: tubing pressure.
19. Choke — Flow control device. Not this: process upset.
20. Commingled Stream — Combined flow from multiple sources. Not this: blended product post-processing.
21. Compressor — Gas compression equipment. Not this: file compressor.
22. Condensate — Hydrocarbon liquid from gas. Synonyms: NGL liquids. Not this: water condensation in HVAC.
23. Configuration — System settings under change control. Not this: source code.
24. Control Room — Central operations center. Not this: server room.
25. Corrosion Coupon — Physical probe measuring corrosion rate. Not this: discount coupon.
26. Critical Alarm — Highest severity alarm requiring immediate action. Not this: warning event.
27. Crude Oil — Unrefined petroleum. Not this: refined products.
28. Custody Transfer — Legal change of product ownership at metering point. Not this: internal handoff.
29. Daily Production — Total produced volume per day. SI: m³/day (oil), m³/day@std (gas). Not this: sales volume.
30. Data Lake — Object storage for large datasets. Not this: ADX time-series.
31. Dehydrator — Equipment removing water from gas. Not this: lab desiccator.
32. Differential Pressure — Pressure difference across element (Pa). Not this: static pressure.
33. Downhole Gauge — Sensor measuring downhole pressure/temperature. Not this: surface gauge.
34. Downstream (DDD) — Context consuming events of another. Not this: refining downstream sector.
35. Drillstem Test — Pressure/flow test during drilling. Not this: well test in production phase.
36. ESP — Electric submersible pump. Not this: extrasensory perception.
37. Event — Immutable fact that something occurred. Not this: future intention (command).
38. Event Hub — Azure messaging service for streaming ingestion. Not this: Kafka (though conceptually similar).
39. Event Sourcing — Persisting state as sequence of events. Not this: log aggregation.
40. Exception (Ops) — Non-routine condition requiring review. Not this: software exception.
41. Facility — Physical site where processing occurs. Not this: corporate building.
42. Factor (Meter) — Calibration/proving derived multiplier. Not this: allocation factor.
43. Fan-out — Propagation of events to multiple consumers. Not this: fan speed.
44. Flaring — Controlled burning of gas. Not this: flare nut in piping.
45. Flow Computer — Device computing corrected flow from measurements. Not this: SCADA server.
46. Flowline — Pipe from well to facility. Not this: pipeline trunk.
47. Formation Gas — Gas produced from reservoir. Not this: instrument gas.
48. Gas-Lift — Artificial lift injecting gas. Not this: nitrogen lift in labs.
49. Gauge Pressure — Pressure relative to ambient. SI: Pa. Not this: absolute pressure.
50. Gathering System — Network collecting production to central point. Not this: transmission pipeline.
51. GOR — Gas-oil ratio. SI: m³/m³ at standard conditions. Not this: mass ratio unless specified.
52. H2S — Hydrogen sulfide; toxic gas. Not this: HS2 rail.
53. HAZOP — Hazard and operability study. Not this: generic risk assessment.
54. Heater-Treater — Equipment separating oil/water/gas. Not this: stabilizer.
55. Hot Path — Real-time processing path. Not this: hot storage tier.
56. Hydraulic Fracture — Stimulation of reservoir. Not this: natural fracture mapping.
57. IDP — Identity provider (Azure AD). Not this: immigration IDP.
58. Imbalance — Difference between measured in/out. Not this: accounting imbalance.
59. Integrity Management — Program for preventing failures (e.g., corrosion). Not this: data integrity only.
60. Interlock — Safety control preventing operation. Not this: UI lock.
61. Isothermal Compressibility — Reservoir property. Not this: thermal expansion.
62. KPI — Key performance indicator. Not this: generic metric without target.
63. Key Vault — Managed secrets/certificates store. Not this: password file.
64. LACT Unit — Lease Automatic Custody Transfer unit. Not this: lab lactate test.
65. Leak Detection — Detection of loss from pipeline via models/signals. Not this: seep at facility unless included.
66. Lift Optimization — Process of maximizing production via lift settings. Not this: capex optimization.
67. Line Pack — Gas stored in pipeline due to pressure. Not this: packaging line.
68. LOPA — Layers of protection analysis. Not this: generic risk matrix.
69. MAOP — Maximum allowable operating pressure. Not this: burst pressure.
70. Manifold — Piping assembly combining flows. Not this: exhaust manifold in autos.
71. Measurement Uncertainty — Combined error of measurement. Not this: model error.
72. MediatR — .NET mediator library. Not this: media player.
73. Meter Factor — Proving-derived correction factor. Not this: allocation factor.
74. Meter Proving — Procedure to validate meter accuracy. Not this: device calibration only.
75. Meter Run — Segment housing orifice/turbine meters. Not this: production run.
76. Mitigation — Action to reduce risk. Not this: insurance.
77. Modeling Drift — Degradation of ML model performance over time. Not this: configuration drift.
78. NER — Net error ratio in proving. Not this: NLP named entity recognition.
79. NGL — Natural gas liquids. Not this: LPG specifically unless subset.
80. Nomination — Scheduled movement/volume commitment. Not this: award nomination.
81. Opex — Operating expense. Not this: capex.
82. OPC — OLE for Process Control protocol. Not this: OPC UA vs classic must be specified.
83. Operator (Role) — Field operator user. Not this: company operator.
84. Orifice Plate — Primary element for DP measurement. Not this: valve orifice generally.
85. OT — Operational technology (SCADA, RTUs). Not this: overtime.
86. Outage — Unavailability outside SLO. Not this: planned maintenance window.
87. Over-Read — Meter reading bias high. Not this: over-read as overbook.
88. Peak Shaving — Reducing peak demand. Not this: load shedding in power only.
89. Permit to Work (PTW) — Formal authorization for hazardous work. Not this: HR work permit.
90. Pigging — Pipeline cleaning/inspection with pigs. Not this: livestock.
91. Plunger Lift — Artificial lift method using plunger. Not this: gas-lift.
92. Pressure — Force per unit area. SI: Pa. Not this: psi without conversion.
93. Pressure Drop — Difference across a component. SI: Pa. Not this: altitude pressure change.
94. Proppant — Material to keep fractures open. Not this: catalyst.
95. Proving Ticket — Record of proving session results. Not this: service ticket.
96. Pump — Mechanical device to move fluids. Not this: heat pump.
97. PV (Process Variable) — Measured variable. Not this: present value in finance.
98. QA/QC — Quality assurance/control activities. Not this: QA in software only.
99. Rate — Volume/time or mass/time. Units must include time base. Not this: price rate.
100. RTO/RPO — Recovery time/point objectives. Not this: SLA.
101. RTU — Remote terminal unit. Not this: return-to-utility.
102. Sample — Collected fluid for lab analysis. Not this: example.
103. SCADA — Supervisory control and data acquisition. Not this: DCS unless specified.
104. Separator — Equipment separating phases. Not this: filter only.
105. Setpoint — Target value for control loop. Not this: business target.
106. Shut-In — Valve closure to stop flow. Not this: economic shut-in only.
107. SLO — Service level objective: measurable target. Not this: SLA (contract).
108. Slug — Intermittent large volume in flow. Not this: animal slug.
109. Sorbent — Material used for absorption/adsorption. Not this: solvent.
110. SPCC — Spill prevention, control, and countermeasure plan. Not this: generic spill plan.
111. Static Pressure — Absolute or gauge depending context. Units required. Not this: DP.
112. Stick Measurement — Manual tank gauging. Not this: automated radar.
113. Surging — Rapid pressure fluctuation. Not this: surge pricing.
114. Surveillance — Continuous monitoring and analysis. Not this: security surveillance.
115. Tank Strapping — Calibration of tank volume vs height. Not this: cargo strapping.
116. Tap — Connection to pipeline/line. Not this: tap water.
117. Telemetry — Remote measurement data. Not this: logs.
118. Temperature — Thermal state. SI: K/°C. Not this: °F without conversion.
119. Tenant — Logical customer in multi-tenant system. Not this: end user.
120. Throughput — Amount processed per time. Not this: bandwidth unless specified.
121. Ticket (Custody) — Legally binding record of transferred quantity. Not this: trouble ticket.
122. Timescale/ADX — Time-series analytics store. Not this: transactional DB.
123. Tubing Pressure — Pressure in production tubing. Not this: casing pressure.
124. Turbine Meter — Velocity-type flow meter. Not this: orifice plate.
125. UOM — Unit of measure; ISO default. Not this: user of month.
126. Upset — Process condition outside normal range. Not this: user upset.
127. Upstream (DDD) — Producer of events for other contexts. Not this: industry upstream sector.
128. Vapor Pressure — Pressure at which liquid vaporizes. Not this: vapor density.
129. Variance (Regulatory) — Approved deviation from requirement. Not this: statistical variance.
130. VCF — Volume correction factor for temperature/pressure. Not this: version control file.
131. Venting — Release of gas to atmosphere. Not this: vent in HVAC.
132. Viscosity — Fluid resistance to flow (Pa·s). Not this: kinematic unless specified.
133. VSD/VFD — Variable speed/frequency drive. Not this: UPS.
134. Water Cut — Fraction of water in liquid stream. Units: %. Not this: water production rate.
135. Well — Borehole producing fluids. Not this: injection well unless specified.
136. Well Test — Short-term test to estimate rates. Not this: DST during drilling.
137. WHP — Wellhead pressure. Not this: bottomhole pressure.
138. Work Order — Instruction for work to be performed. Not this: sales order.
139. WORM — Write once, read many storage for immutability. Not this: computer worm.
140. Yield (Gas) — NGL yield per gas volume. Not this: financial yield.
141. Z-Factor — Gas compressibility factor. Not this: statistical z-score.
142. Coriolis Meter — Mass flow meter. Not this: thermal mass meter.
143. Orifice Flow Equation — ISO 5167 DP flow calc. Not this: empirical curve only.
144. Standard Conditions — Reference T/P (e.g., 15°C, 101.325 kPa). Not this: ambient unless specified.
145. Meter Run Temperature — Temperature at meter element. Not this: ambient temperature.
146. Custody Point — Defined point where ownership changes. Not this: internal handoff.
147. Leak Rate — Estimated rate of product loss. Units: m³/s. Not this: percent loss unless specified.
148. False Positive Rate — Proportion of incorrect leak alarms. Not this: precision.
149. Permit Isolation — Mechanical/electrical isolation state. Not this: network isolation.
150. Jurisdiction Profile — Parameterized regulatory ruleset. Not this: generic config profile.
151. Feature Window — Derived features over time window. Not this: UI window.
152. Downsampling — Reducing data resolution. Not this: compression.
153. Shadow Deployment — Non-user-facing deployment for validation. Not this: canary if not routing.
154. Dead-Letter Queue — Queue for failed messages. Not this: trash.
155. Envelope Encryption — Encrypting data keys with master key. Not this: email encryption.
156. Error Budget — Allowed unreliability against SLO. Not this: financial budget.
157. ABAC — Attribute-based access control. Not this: RBAC only.
158. RBAC — Role-based access control. Not this: ABAC.
159. PII — Personally identifiable information. Not this: sensitive operational IP.
160. Data Lineage — Traceability of data transformations. Not this: git history.


